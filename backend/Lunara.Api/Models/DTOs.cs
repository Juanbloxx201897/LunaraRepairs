using System.ComponentModel.DataAnnotations;

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
/// <summary>Identifies a catalog item and quantity to include in a repair estimate.</summary>
public sealed record RepairBudgetPartDto(int SparePartId, int Quantity);

/// <summary>Diagnosis and price inputs used by the assigned technician to prepare an estimate.</summary>
public record DiagnosisDto(
    string Diagnosis,
    decimal Budget,
    string Note,
    decimal LaborCost = 0,
    IReadOnlyList<RepairBudgetPartDto>? Parts = null,
    string WorkType = "");

/// <summary>Reports whether the saved estimate was delivered to the customer by email.</summary>
public sealed record BudgetSubmissionResponse(bool EmailSent, string Message);

/// <summary>Reports whether the ready-for-pickup notification was delivered by email.</summary>
public sealed record ReadyNotificationResponse(bool EmailSent, string Message);

/// <summary>Catalog pricing and stock details exposed to technicians preparing estimates.</summary>
public sealed record SparePartCatalogDto(int Id, string Name, string Category, string Brand, int Quantity, decimal UnitPrice);

/// <summary>Labor price for a device type, brand, and repair task.</summary>
public sealed record LaborPriceGuideEntryResponse(
    int Id,
    string DeviceType,
    string Brand,
    string WorkType,
    decimal Price,
    bool IsActive,
    bool IsSample);

/// <summary>Fields used to create or update an administrator-managed labor price.</summary>
public sealed record SaveLaborPriceGuideEntryRequest
{
    [Required, MaxLength(80)]
    public required string DeviceType { get; init; }

    [Required, MaxLength(80)]
    public required string Brand { get; init; }

    [Required, MaxLength(120)]
    public required string WorkType { get; init; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal Price { get; init; }

    public bool IsActive { get; init; } = true;

    public bool IsSample { get; init; }
}

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
