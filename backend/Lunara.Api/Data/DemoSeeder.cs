using Lunara.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Data;

public static class DemoSeeder
{
    public static async Task SeedAsync(LunaraDbContext db)
    {
        if (await db.Clients.AnyAsync()) return;

        var clients = new List<Client>
        {
            new() { Cedula = "900000001", FullName = "Laura Gómez", Email = "laura@example.test", Phone = "+573001112233" },
            new() { Cedula = "900000002", FullName = "Andrés Pérez", Email = "andres@example.test", Phone = "+573002223344" },
            new() { Cedula = "900000003", FullName = "Valentina Ruiz", Email = "valentina@example.test", Phone = "+573003334455" },
            new() { Cedula = "900000004", FullName = "Mateo Torres", Email = "mateo@example.test", Phone = "+573004445566" },
            new() { Cedula = "900000005", FullName = "Camila Rojas", Email = "camila@example.test", Phone = "+573005556677" }
        };
        var techs = new List<Technician>
        {
            new() { FullName = "Santiago Mejía", Email = "santiago@example.test" },
            new() { FullName = "Daniela Castro", Email = "daniela@example.test" },
            new() { FullName = "Nicolás Gil", Email = "nicolas@example.test" }
        };
        db.Clients.AddRange(clients);
        db.Technicians.AddRange(techs);
        await db.SaveChangesAsync();

        var devices = new List<Device>
        {
            new() { ClientId = clients[0].Id, Type = "Celular", Brand = "Samsung", Model = "Galaxy A54", SerialNumber = "DEMO-001", Description = "Pantalla con fallas" },
            new() { ClientId = clients[1].Id, Type = "Portátil", Brand = "Lenovo", Model = "IdeaPad 3", SerialNumber = "DEMO-002", Description = "No carga" },
            new() { ClientId = clients[2].Id, Type = "Consola", Brand = "Sony", Model = "PlayStation 4", SerialNumber = "DEMO-003", Description = "Se sobrecalienta" },
            new() { ClientId = clients[3].Id, Type = "Celular", Brand = "Apple", Model = "iPhone 12", SerialNumber = "DEMO-004", Description = "Batería dura poco" },
            new() { ClientId = clients[4].Id, Type = "Tableta", Brand = "Samsung", Model = "Galaxy Tab", SerialNumber = "DEMO-005", Description = "Puerto de carga" },
            new() { ClientId = clients[0].Id, Type = "Portátil", Brand = "HP", Model = "Pavilion", SerialNumber = "DEMO-006", Description = "Mantenimiento general" }
        };
        db.Devices.AddRange(devices);
        await db.SaveChangesAsync();

        var statuses = new[]
        {
            RepairStatuses.Received, RepairStatuses.Diagnosing, RepairStatuses.PendingApproval,
            RepairStatuses.Approved, RepairStatuses.Repairing, RepairStatuses.Testing,
            RepairStatuses.Ready, RepairStatuses.Delivered
        };
        for (var i = 0; i < 8; i++)
        {
            var device = devices[i % devices.Count];
            var order = new RepairOrder
            {
                Code = $"LR-{DateTime.UtcNow:yyyy}-{1001 + i}",
                ClientId = device.ClientId,
                DeviceId = device.Id,
                TechnicianId = techs[i % techs.Count].Id,
                ProblemDescription = device.Description,
                Diagnosis = i >= 2 ? "Revisión inicial realizada; se requiere validación técnica." : "",
                Budget = i >= 2 ? 150000m + (i * 12500m) : null,
                Status = statuses[i],
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                UpdatedAt = DateTime.UtcNow.AddHours(-i)
            };
            db.RepairOrders.Add(order);
            await db.SaveChangesAsync();
            db.RepairStatusHistory.Add(new RepairStatusHistory
            {
                RepairOrderId = order.Id, PreviousStatus = null, NewStatus = order.Status,
                Note = "Orden creada para demostración", ChangedAt = order.CreatedAt
            });
        }
        await db.SaveChangesAsync();
    }
}
