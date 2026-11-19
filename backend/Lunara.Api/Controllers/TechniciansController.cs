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
// Proporciona el listado de técnicos activos para asignarlos a una reparación.
public class TechniciansController(
    LunaraDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    [Authorize(Roles = "Admin,Administrator")]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var technicians = await db.Technicians.AsNoTracking()
            .OrderBy(x => x.FullName).ToListAsync();
        var workload = await db.RepairOrders.AsNoTracking()
            .Where(x => x.TechnicianId != null &&
                        x.Status != RepairStatuses.Delivered &&
                        x.Status != RepairStatuses.Rejected)
            .GroupBy(x => x.TechnicianId)
            .Select(group => new { TechnicianId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.TechnicianId!.Value, x => x.Count);
        var accounts = await db.AuthUsers.AsNoTracking()
            .Where(x => x.TechnicianId != null)
            .ToDictionaryAsync(x => x.TechnicianId!.Value, x => x);

        return Ok(technicians.Select(technician =>
        {
            var count = workload.GetValueOrDefault(technician.Id);
            accounts.TryGetValue(technician.Id, out var account);
            return new
            {
                technician.Id,
                technician.FullName,
                technician.Email,
                Username = account is null ? null : UsernameFromEmail(account.Email),
                technician.Active,
                HasAccount = account is not null,
                AccountActive = account?.IsActive ?? false,
                Workload = count,
                Availability = !technician.Active || account is { IsActive: false }
                    ? "Inactivo"
                    : account is null ? "Sin cuenta"
                    : count >= 4 ? "Alta carga" : count >= 2 ? "Ocupado" : "Disponible"
            };
        }));
    }

    [Authorize(Roles = "Admin,Administrator")]
    [HttpPost]
    public async Task<IActionResult> Create(TechnicianCreateDto dto)
    {
        var fullName = dto.FullName.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();
        if (fullName.Length < 2 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest(new { message = "Escribe el nombre completo y un correo válido." });
        if (dto.Password.Length < 6)
            return BadRequest(new { message = "La contraseña del técnico debe tener al menos 6 caracteres." });
        var expectedSecret = TechnicianManagementSecret();
        if (string.IsNullOrEmpty(expectedSecret))
            return StatusCode(503, new { message = "Configura Admin:TechnicianCreationSecret en el servidor antes de crear técnicos." });
        if (!SecretsMatch(dto.Secret, expectedSecret))
            return Unauthorized(new { message = "La contraseña secreta de administración no es válida." });
        if (await db.AuthUsers.AnyAsync(x => x.Email == email) ||
            await db.Technicians.AnyAsync(x => x.Email.ToLower() == email))
            return Conflict(new { message = "Ya existe una cuenta con ese correo." });
        var username = UsernameFromEmail(email);
        var technicianAccounts = await db.AuthUsers
            .Where(x => x.Role == "Technician")
            .Select(x => x.Email)
            .ToListAsync();
        if (technicianAccounts.Any(existingEmail =>
                string.Equals(
                    UsernameFromEmail(existingEmail),
                    username,
                    StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = "Ya existe un técnico con ese usuario (la parte anterior a @ del correo)." });

        var technician = new Technician { FullName = fullName, Email = email, Active = true };
        var account = new AuthUser
        {
            Email = email,
            Role = "Technician",
            PasswordHash = PasswordService.Hash(dto.Password),
            IsActive = true
        };
        db.Technicians.Add(technician);
        await db.SaveChangesAsync();
        account.TechnicianId = technician.Id;
        db.AuthUsers.Add(account);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = technician.Id },
            new { technician.Id, technician.FullName, technician.Email, username, technician.Active, hasAccount = true, accountActive = true, workload = 0, availability = "Disponible" });
    }

    [Authorize(Roles = "Admin,Administrator")]
    [HttpPut("{id:int}/access")]
    public async Task<IActionResult> UpdateAccess(int id, TechnicianAccessDto dto)
    {
        var technician = await db.Technicians.FirstOrDefaultAsync(x => x.Id == id);
        if (technician is null) return NotFound(new { message = "No se encontró el técnico." });
        var account = await db.AuthUsers.FirstOrDefaultAsync(x => x.TechnicianId == id);
        if (!string.IsNullOrWhiteSpace(dto.NewPassword) && dto.NewPassword.Length < 6)
            return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });

        if (account is null)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Escribe una contraseña inicial para crear el acceso del técnico." });
            if (string.IsNullOrWhiteSpace(dto.Secret) ||
                !SecretsMatch(dto.Secret, TechnicianManagementSecret() ?? ""))
                return Unauthorized(new { message = "La clave secreta de administración no es válida." });

            var email = technician.Email.Trim().ToLowerInvariant();
            var username = UsernameFromEmail(email);
            var existingTechnicianEmails = await db.AuthUsers
                .Where(x => x.Role == "Technician")
                .Select(x => x.Email)
                .ToListAsync();
            if (await db.AuthUsers.AnyAsync(x => x.Email == email) ||
                existingTechnicianEmails.Any(existingEmail =>
                    string.Equals(
                        UsernameFromEmail(existingEmail),
                        username,
                        StringComparison.OrdinalIgnoreCase)))
                return Conflict(new { message = "Ya existe una cuenta con ese correo o usuario." });

            technician.Active = true;
            account = new AuthUser
            {
                Email = email,
                Role = "Technician",
                PasswordHash = PasswordService.Hash(dto.NewPassword),
                TechnicianId = id,
                IsActive = true
            };
            db.AuthUsers.Add(account);
            await db.SaveChangesAsync();
            return NoContent();
        }

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var expectedSecret = TechnicianManagementSecret();
            if (string.IsNullOrWhiteSpace(dto.Secret) ||
                string.IsNullOrWhiteSpace(expectedSecret) ||
                !SecretsMatch(dto.Secret, expectedSecret))
                return Unauthorized(new { message = "La clave secreta de administración no es válida." });
        }

        var wasActive = account.IsActive;
        technician.Active = dto.Active;
        account.IsActive = dto.Active;
        if (wasActive != dto.Active || !string.IsNullOrWhiteSpace(dto.NewPassword))
            account.SessionKey = Guid.NewGuid().ToString("N");
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            account.PasswordHash = PasswordService.Hash(dto.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private string? TechnicianManagementSecret() =>
        environment.IsDevelopment()
            ? "admin"
            : configuration["Admin:TechnicianCreationSecret"];

    private static bool SecretsMatch(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static string UsernameFromEmail(string email) =>
        email.Split('@', 2)[0];
}
