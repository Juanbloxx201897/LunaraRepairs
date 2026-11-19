using Lunara.Api.Models;
using Lunara.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Data;

// Carga cuentas de desarrollo y las vincula con los clientes o técnicos existentes.
public static class AuthSeeder
{
    public static async Task SeedFromCsvAsync(
        LunaraDbContext db,
        string csvPath,
        string? adminInitialPassword = null,
        bool resetExistingAdminPassword = false)
    {
        if (!File.Exists(csvPath))
        {
            var admin = await db.AuthUsers.FirstOrDefaultAsync(
                x => x.Role == "Admin" || x.Role == "Administrator");
            if (admin is null)
            {
                if (string.IsNullOrWhiteSpace(adminInitialPassword))
                    throw new InvalidOperationException(
                        "No hay una cuenta de administrador. Configura Admin:InitialPassword.");

                db.AuthUsers.Add(new AuthUser
                {
                    Email = "admin@lunara.local",
                    Role = "Admin",
                    PasswordHash = PasswordService.Hash(adminInitialPassword)
                });
            }
            else if (resetExistingAdminPassword &&
                     !string.IsNullOrWhiteSpace(adminInitialPassword))
            {
                admin.PasswordHash = PasswordService.Hash(adminInitialPassword);
                admin.SessionKey = Guid.NewGuid().ToString("N");
            }

            await db.SaveChangesAsync();
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);

        // La primera fila contiene encabezados; las demás se convierten en usuarios si aún no existen.
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Este CSV de desarrollo usa campos simples sin comas internas.
            var columns = line.Split(',');
            if (columns.Length != 4)
                throw new InvalidDataException(
                    $"Fila inválida en el CSV: {line}");

            var email = columns[0].Trim().ToLowerInvariant();
            var role = columns[1].Trim();
            var linkedEmail = columns[2].Trim().ToLowerInvariant();
            var password = columns[3];

            if (email.Length == 0 ||
                (password.Length == 0 && !(role == "Admin" && !string.IsNullOrWhiteSpace(adminInitialPassword))))
                throw new InvalidDataException(
                    "Hay una cuenta sin correo o contraseña.");

            if (role == "Admin" &&
                !string.IsNullOrWhiteSpace(adminInitialPassword))
            {
                if (adminInitialPassword.Length < 12 && !resetExistingAdminPassword)
                    throw new InvalidOperationException(
                        "Admin:InitialPassword debe tener al menos 12 caracteres.");
                password = adminInitialPassword;
            }

            if (role is not ("Admin" or "Technician" or "Client"))
                throw new InvalidDataException(
                    $"Rol no válido para {email}: {role}");

            var existingUser = await db.AuthUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (existingUser is not null)
            {
                if (role == "Admin" && resetExistingAdminPassword &&
                    !string.IsNullOrWhiteSpace(adminInitialPassword))
                {
                    existingUser.PasswordHash = PasswordService.Hash(adminInitialPassword);
                    existingUser.SessionKey = Guid.NewGuid().ToString("N");
                    await db.SaveChangesAsync();
                }
                continue;
            }

            int? clientId = null;
            int? technicianId = null;

            if (role == "Client")
            {
                var client = await db.Clients.FirstOrDefaultAsync(
                    x => x.Email.ToLower() == linkedEmail);

                if (client is null)
                    throw new InvalidDataException(
                        $"No se encontró el cliente vinculado a {email}.");

                clientId = client.Id;
            }
            else if (role == "Technician")
            {
                var technician = await db.Technicians.FirstOrDefaultAsync(
                    x => x.Email.ToLower() == linkedEmail);

                if (technician is null)
                    throw new InvalidDataException(
                        $"No se encontró el técnico vinculado a {email}.");

                technicianId = technician.Id;
            }

            db.AuthUsers.Add(new AuthUser
            {
                Email = email,
                Role = role,
                PasswordHash = PasswordService.Hash(password),
                ClientId = clientId,
                TechnicianId = technicianId
            });

            await db.SaveChangesAsync();
        }
    }
}
