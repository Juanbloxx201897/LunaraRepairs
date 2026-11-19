using System.ComponentModel.DataAnnotations;
using Lunara.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Data;

// Importa clientes ficticios completos para que la búsqueda pueda probarse con un directorio amplio.
public static class ClientDemoSeeder
{
    public static async Task SeedFromCsvAsync(LunaraDbContext db, string csvPath)
    {
        if (!File.Exists(csvPath))
            return;

        var lines = await File.ReadAllLinesAsync(csvPath);
        if (lines.Length == 0 || lines[0] != "cedula,full_name,email,phone")
            throw new InvalidDataException("El CSV de clientes debe incluir las columnas cedula,full_name,email,phone.");

        var seenCedulas = new HashSet<string>(StringComparer.Ordinal);
        var technicians = await db.Technicians.Where(x => x.Active).OrderBy(x => x.Id).ToListAsync();
        for (var index = 1; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
                continue;

            var columns = lines[index].Split(',');
            if (columns.Length != 4)
                throw new InvalidDataException($"Fila {index + 1} inválida en el CSV de clientes.");

            var cedula = NormalizeCedula(columns[0]);
            var fullName = columns[1].Trim();
            var email = columns[2].Trim().ToLowerInvariant();
            var phone = ClientPhone.Normalize(columns[3]);

            if (cedula is null || string.IsNullOrWhiteSpace(fullName) ||
                !new EmailAddressAttribute().IsValid(email) ||
                phone is null)
                throw new InvalidDataException($"La fila {index + 1} del CSV tiene campos obligatorios incompletos o inválidos.");

            if (!seenCedulas.Add(cedula))
                throw new InvalidDataException($"La cédula de la fila {index + 1} está repetida en el CSV de clientes.");

            var client = await db.Clients.FirstOrDefaultAsync(x => x.Cedula == cedula);
            if (client is not null)
            {
                if (ClientPhone.Normalize(client.Phone) is null)
                    client.Phone = phone;
                if (await db.RepairOrders.AnyAsync(x => x.ClientId == client.Id))
                    continue;
            }

            client ??= new Client
            {
                Cedula = cedula,
                FullName = fullName,
                Email = email,
                Phone = phone
            };
            var device = new Device
            {
                Client = client,
                Type = "Celular",
                Brand = "Samsung",
                Model = $"Galaxy A{(index % 25) + 1}",
                SerialNumber = $"DEMO-CSV-{cedula}",
                Description = "Equipo de prueba para historial de servicio"
            };
            var createdAt = DateTime.UtcNow.AddDays(-30 - index);
            var repair = new RepairOrder
            {
                Client = client,
                Device = device,
                Technician = technicians.Count == 0 ? null : technicians[index % technicians.Count],
                Code = $"LR-DEMO-{cedula}",
                ProblemDescription = "Mantenimiento preventivo y revisión general",
                Diagnosis = "Equipo revisado; se realizó mantenimiento preventivo.",
                WorkPerformed = "Limpieza interna, revisión de componentes y pruebas funcionales.",
                Budget = 85000m,
                Status = RepairStatuses.Delivered,
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddDays(2)
            };

            var completedStatuses = new[]
            {
                RepairStatuses.Received, RepairStatuses.Diagnosing,
                RepairStatuses.PendingApproval, RepairStatuses.Approved,
                RepairStatuses.Repairing, RepairStatuses.Testing,
                RepairStatuses.Ready, RepairStatuses.Delivered
            };
            for (var statusIndex = 0; statusIndex < completedStatuses.Length; statusIndex++)
            {
                repair.History.Add(new RepairStatusHistory
                {
                    PreviousStatus = statusIndex == 0 ? null : completedStatuses[statusIndex - 1],
                    NewStatus = completedStatuses[statusIndex],
                    Note = statusIndex == completedStatuses.Length - 1
                        ? "Servicio de prueba entregado al cliente."
                        : "Etapa de servicio de prueba completada.",
                    ChangedAt = createdAt.AddHours(statusIndex * 6)
                });
            }

            db.RepairOrders.Add(repair);
        }

        await db.SaveChangesAsync();
    }

    private static string? NormalizeCedula(string value)
    {
        var normalized = value.Trim().Replace(".", "").Replace("-", "").Replace(" ", "");
        return normalized.Length is >= 5 and <= 20 &&
               normalized.All(character => character is >= '0' and <= '9')
            ? normalized
            : null;
    }
}
