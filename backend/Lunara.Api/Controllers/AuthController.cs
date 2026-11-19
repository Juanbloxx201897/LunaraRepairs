using Lunara.Api.Data;
using Lunara.Api.Models;
using Lunara.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Lunara.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Verifica las credenciales y entrega el token que el frontend usa en las solicitudes autenticadas.
public class AuthController(
    LunaraDbContext db,
    JwtTokenService tokenService,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    // Comprueba las credenciales dentro del perfil elegido para evitar cruces de roles.
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var identifier = (dto.Username ?? dto.Email)?.Trim();
        var clientCedulaLogin =
            dto.Role?.Trim() == "Client" &&
            configuration.GetValue("Client:AllowCedulaOnlyLogin", environment.IsDevelopment());
        if (string.IsNullOrWhiteSpace(identifier) ||
            (!clientCedulaLogin && string.IsNullOrEmpty(dto.Password)))
        {
            return BadRequest(new
            {
                message = clientCedulaLogin
                    ? "La cédula es obligatoria."
                    : "Usuario y contraseña son obligatorios."
            });
        }

        AuthUser? user;
        var selectedRole = dto.Role?.Trim();
        switch (selectedRole)
        {
            case "Admin":
                var adminUsername = configuration["Admin:Username"] ?? "admin";
                user = string.Equals(identifier, adminUsername, StringComparison.OrdinalIgnoreCase)
                    ? await db.AuthUsers.FirstOrDefaultAsync(
                        x => x.Role == "Admin" || x.Role == "Administrator")
                    : null;
                break;
            case "Technician":
                var technicianAccounts = await db.AuthUsers
                    .Where(x => x.Role == "Technician")
                    .ToListAsync();
                var matches = technicianAccounts
                    .Where(x => string.Equals(
                        UsernameFromEmail(x.Email),
                        identifier,
                        StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();
                user = matches.Count == 1 ? matches[0] : null;
                break;
            case "Client":
                if (clientCedulaLogin)
                {
                    var client = await db.Clients.FirstOrDefaultAsync(
                        x => x.Cedula == identifier);
                    if (client is null)
                        return Unauthorized(new { message = "No se encontró un cliente con esa cédula." });

                    user = await db.AuthUsers.FirstOrDefaultAsync(
                        x => x.Role == "Client" && x.ClientId == client.Id);
                    if (user is null)
                    {
                        var email = client.Email;
                        if (await db.AuthUsers.AnyAsync(x => x.Email == email))
                            email = $"cliente-{client.Id}@lunara.local";

                        user = new AuthUser
                        {
                            Email = email,
                            Role = "Client",
                            PasswordHash = PasswordService.Hash(Guid.NewGuid().ToString("N")),
                            ClientId = client.Id
                        };
                        db.AuthUsers.Add(user);
                        await db.SaveChangesAsync();
                    }
                }
                else
                {
                    user = await db.AuthUsers.FirstOrDefaultAsync(
                        x => x.Role == "Client" && x.Email == identifier.ToLowerInvariant());
                }
                break;
            case null or "":
                user = await db.AuthUsers.FirstOrDefaultAsync(
                    x => x.Email == identifier.ToLowerInvariant());
                break;
            default:
                return BadRequest(new { message = "Selecciona un perfil de acceso válido." });
        }

        if (user is null ||
            !user.IsActive ||
            (!clientCedulaLogin &&
             (string.IsNullOrEmpty(dto.Password) ||
              !PasswordService.Verify(dto.Password, user.PasswordHash))))
        {
            return Unauthorized(new
            {
                message = "Usuario o contraseña incorrectos."
            });
        }
        if (user.TechnicianId is int technicianId &&
            !await db.Technicians.AnyAsync(x => x.Id == technicianId && x.Active))
        {
            return Unauthorized(new {
                message = "La cuenta del técnico está desactivada."
            });
        }

        var token = tokenService.CreateToken(user);
        var username = user.Role is "Admin" or "Administrator"
            ? configuration["Admin:Username"] ?? "admin"
            : user.Role == "Technician"
                ? UsernameFromEmail(user.Email)
                : user.Role == "Client" && user.ClientId is int clientId
                    ? await db.Clients.Where(x => x.Id == clientId)
                        .Select(x => x.Cedula).FirstOrDefaultAsync() ?? user.Email
                : user.Email;
        var displayName = user.TechnicianId is int linkedTechnicianId
            ? await db.Technicians.Where(x => x.Id == linkedTechnicianId)
                .Select(x => x.FullName).FirstOrDefaultAsync() ?? username
            : user.ClientId is int linkedClientId
                ? await db.Clients.Where(x => x.Id == linkedClientId)
                    .Select(x => x.FullName).FirstOrDefaultAsync() ?? username
                : "Administrador";

        return Ok(new LoginResponseDto(
            token,
            user.Email,
            user.Role,
            user.ClientId,
            user.TechnicianId,
            username,
            displayName
        ));
    }

    [AllowAnonymous]
    [HttpPost("admin/recover")]
    public async Task<IActionResult> RecoverAdminPassword(AdminPasswordRecoveryDto dto)
    {
        var configuredUsername = configuration["Admin:Username"] ?? "admin";
        var recoverySecret = configuration["Admin:RecoverySecret"];
        if (string.IsNullOrWhiteSpace(dto.Username) ||
            string.IsNullOrEmpty(dto.RecoverySecret) ||
            string.IsNullOrEmpty(dto.NewPassword))
            return BadRequest(new { message = "Completa usuario, secreto de recuperación y contraseña nueva." });
        if (string.IsNullOrWhiteSpace(recoverySecret))
            return StatusCode(503, new
            {
                message = "La recuperación no está habilitada. Configura Admin:RecoverySecret en el servidor."
            });

        if (!string.Equals(dto.Username.Trim(), configuredUsername, StringComparison.OrdinalIgnoreCase) ||
            !SecretsMatch(dto.RecoverySecret, recoverySecret))
            return Unauthorized(new { message = "El usuario o el secreto de recuperación no son válidos." });
        if (dto.NewPassword.Length < 12)
            return BadRequest(new { message = "La nueva contraseña debe tener al menos 12 caracteres." });

        var admin = await db.AuthUsers.FirstOrDefaultAsync(
            x => x.Role == "Admin" || x.Role == "Administrator");
        if (admin is null)
            return NotFound(new { message = "No se encontró la cuenta de administrador." });

        admin.PasswordHash = PasswordService.Hash(dto.NewPassword);
        admin.SessionKey = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string UsernameFromEmail(string email) =>
        email.Split('@', 2)[0];

    private static bool SecretsMatch(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
