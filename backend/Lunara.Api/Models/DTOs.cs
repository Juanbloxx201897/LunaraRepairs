namespace Lunara.Api.Models;

// DTOs: formas de entrada específicas para la API; evitan pedir al cliente que envíe entidades completas.
public record RepairRegistrationDto(
    string Cedula,
    string? FullName,
    string? Email,
    string? Phone,
    string Type,
    string Brand,
    string Model,
    string SerialNumber,
    string DeviceDescription,
    string ProblemDescription,
    int? TechnicianId
);
public record ClientRepairHistoryDto(
    int Id,
    string Code,
    string Device,
    string ProblemDescription,
    string Status,
    DateTime CreatedAt
);
public record DeviceCreateDto(int ClientId, string Type, string Brand, string Model, string SerialNumber, string Description);
public record SparePartCreateDto(string Name, string Category, string Brand, string Description, int? Quantity, decimal? UnitPrice);
public record RepairCreateDto(int ClientId, int DeviceId, int? TechnicianId, string ProblemDescription);
public record RepairAssignmentDto(int? TechnicianId);
public record DiagnosisDto(string Diagnosis, decimal Budget, string Note);
public record StatusUpdateDto(string Status, string Note);
public record WorkPerformedDto(string WorkPerformed, string Note);
public record TechnicianCreateDto(string FullName, string Email, string Password, string Secret);
public record TechnicianAccessDto(bool Active, string? NewPassword, string? Secret = null);
public record SparePartPurchaseCreateDto(
    int? SparePartId,
    string Name,
    string Category,
    string Brand,
    string Description,
    string Supplier,
    int Quantity,
    decimal UnitPrice
);
