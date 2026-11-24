using Lunara.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Data;

public static class DemoSeeder
{
    public static async Task SeedLaborPriceGuideAsync(LunaraDbContext db)
    {
        if (await db.LaborPriceGuideEntries.AnyAsync()) return;

        var sampleEntries = new (string DeviceType, string Brand, string WorkType, decimal Price)[]
        {
            ("Celular", "Apple", "Cambio de pantalla", 120000m),
            ("Celular", "Apple", "Cambio de batería", 80000m),
            ("Celular", "Samsung", "Cambio de pantalla", 105000m),
            ("Celular", "Samsung", "Cambio de batería", 75000m),
            ("Celular", "Xiaomi", "Cambio de pantalla", 90000m),
            ("Celular", "Xiaomi", "Cambio de batería", 65000m),
            ("Portátil", "Apple", "Mantenimiento preventivo", 110000m),
            ("Portátil", "Apple", "Cambio de pantalla", 190000m),
            ("Portátil", "Lenovo", "Mantenimiento preventivo", 85000m),
            ("Portátil", "Lenovo", "Cambio de teclado", 115000m),
            ("Portátil", "HP", "Mantenimiento preventivo", 85000m),
            ("Portátil", "HP", "Cambio de pantalla", 145000m),
            ("Tableta", "Apple", "Cambio de pantalla", 150000m),
            ("Tableta", "Apple", "Cambio de batería", 95000m),
            ("Tableta", "Samsung", "Cambio de pantalla", 125000m),
            ("Tableta", "Samsung", "Cambio de batería", 85000m),
            ("Consola", "Sony", "Limpieza y mantenimiento", 95000m),
            ("Consola", "Sony", "Reparación de puerto HDMI", 160000m),
            ("Consola", "Microsoft", "Limpieza y mantenimiento", 95000m),
            ("Consola", "Microsoft", "Reparación de puerto HDMI", 155000m),
            ("Consola", "Nintendo", "Limpieza y mantenimiento", 85000m),
            ("Consola", "Nintendo", "Reparación de puerto de carga", 140000m),
            ("Computador", "Dell", "Mantenimiento preventivo", 80000m),
            ("Computador", "Dell", "Instalación de sistema operativo", 90000m),
            ("Computador", "Lenovo", "Mantenimiento preventivo", 80000m),
            ("Computador", "Lenovo", "Instalación de sistema operativo", 90000m),
            ("Computador", "Apple", "Mantenimiento preventivo", 120000m),
            ("Computador", "Apple", "Instalación de sistema operativo", 110000m),
            ("Otro", "Multimarca", "Diagnóstico y revisión", 50000m),
            ("Otro", "Multimarca", "Mantenimiento general", 80000m)
        };

        db.LaborPriceGuideEntries.AddRange(sampleEntries.Select(entry =>
            new LaborPriceGuideEntry
            {
                DeviceType = entry.DeviceType,
                Brand = entry.Brand,
                WorkType = entry.WorkType,
                Price = entry.Price,
                IsActive = true,
                IsSample = true
            }));
        await db.SaveChangesAsync();
    }

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
