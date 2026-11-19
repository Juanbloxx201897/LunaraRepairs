namespace Lunara.Api.Models;

// Entidades del dominio que EF Core persiste y relaciona en la base de datos.
public class Client
{
    public int Id { get; set; }
    public string Cedula { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public List<Device> Devices { get; set; } = [];
    public List<RepairOrder> Repairs { get; set; } = [];
}

public class Device
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public string Type { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string Description { get; set; } = "";
}

// Cada repuesto conserva su identidad, clasificación y existencias dentro del inventario.
public class SparePart
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SparePartPurchase
{
    public int Id { get; set; }
    public int? SparePartId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Description { get; set; } = "";
    public string Supplier { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = "Pedido";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PurchasedAt { get; set; }
}

public class Technician
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Active { get; set; } = true;
}

public class RepairOrder
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }
    public string ProblemDescription { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public string WorkPerformed { get; set; } = "";
    public decimal? Budget { get; set; }
    public string Status { get; set; } = RepairStatuses.Received;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<RepairStatusHistory> History { get; set; } = [];
}

// Guarda cada transición para poder reconstruir el seguimiento de una reparación.
public class RepairStatusHistory
{
    public int Id { get; set; }
    public int RepairOrderId { get; set; }
    public RepairOrder? RepairOrder { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public static class RepairStatuses
{
    public const string Received = "Recibido";
    public const string Diagnosing = "Diagnóstico";
    public const string PendingApproval = "Pendiente de aprobación";
    public const string Approved = "Aprobado";
    public const string Repairing = "En reparación";
    public const string Testing = "En pruebas";
    public const string Ready = "Listo para recoger";
    public const string Delivered = "Entregado";
    public const string Rejected = "Rechazado por el cliente";

    // Lista permitida que usa el servicio de flujo al validar cambios de estado.
    public static readonly string[] All =
    [
        Received, Diagnosing, PendingApproval, Approved, Repairing,
        Testing, Ready, Delivered, Rejected
    ];
}
