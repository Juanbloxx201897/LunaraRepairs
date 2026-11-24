using Lunara.Api.Data;
using Lunara.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Controllers;

[ApiController]
[Route("api/laborprices")]
[Authorize]
public sealed class LaborPricesController(LunaraDbContext db) : ControllerBase
{
    /// <summary>Lists active labor prices for technicians and all prices for administrators.</summary>
    [HttpGet]
    [Authorize(Roles = "Technician,Admin,Administrator")]
    public async Task<ActionResult<IReadOnlyList<LaborPriceGuideEntryResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var query = db.LaborPriceGuideEntries.AsNoTracking();
        if (!User.IsInRole("Admin") && !User.IsInRole("Administrator"))
            query = query.Where(entry => entry.IsActive);

        var entries = await query
            .OrderBy(entry => entry.DeviceType)
            .ThenBy(entry => entry.Brand)
            .ThenBy(entry => entry.WorkType)
            .ToListAsync(cancellationToken);

        return Ok(entries.Select(ToResponse).ToList());
    }

    /// <summary>Returns a single labor price from the guide.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Technician,Admin,Administrator")]
    public async Task<ActionResult<LaborPriceGuideEntryResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var entry = await db.LaborPriceGuideEntries.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entry is null ||
            (!entry.IsActive && !User.IsInRole("Admin") && !User.IsInRole("Administrator")))
            return NotFound();

        return Ok(ToResponse(entry));
    }

    /// <summary>Adds a labor price to the guide.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<ActionResult<LaborPriceGuideEntryResponse>> Create(
        SaveLaborPriceGuideEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(request, out var deviceType, out var brand, out var workType))
            return BadRequest(new { message = "Tipo de equipo, marca y tipo de trabajo son obligatorios." });

        if (await HasDuplicateAsync(0, deviceType, brand, workType, cancellationToken))
            return Conflict(new { message = "Ya existe una tarifa para este equipo, marca y tipo de trabajo." });

        var entry = new LaborPriceGuideEntry
        {
            DeviceType = deviceType,
            Brand = brand,
            WorkType = workType,
            Price = request.Price,
            IsActive = request.IsActive,
            IsSample = request.IsSample
        };
        db.LaborPriceGuideEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(entry);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, response);
    }

    /// <summary>Updates a labor price or activates/deactivates the entry.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<ActionResult<LaborPriceGuideEntryResponse>> Update(
        int id,
        SaveLaborPriceGuideEntryRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await db.LaborPriceGuideEntries
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entry is null) return NotFound();

        if (!TryNormalize(request, out var deviceType, out var brand, out var workType))
            return BadRequest(new { message = "Tipo de equipo, marca y tipo de trabajo son obligatorios." });

        if (await HasDuplicateAsync(id, deviceType, brand, workType, cancellationToken))
            return Conflict(new { message = "Ya existe una tarifa para este equipo, marca y tipo de trabajo." });

        entry.DeviceType = deviceType;
        entry.Brand = brand;
        entry.WorkType = workType;
        entry.Price = request.Price;
        entry.IsActive = request.IsActive;
        entry.IsSample = request.IsSample;
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(entry));
    }

    private async Task<bool> HasDuplicateAsync(
        int excludedId,
        string deviceType,
        string brand,
        string workType,
        CancellationToken cancellationToken) =>
        await db.LaborPriceGuideEntries.AnyAsync(entry =>
            entry.Id != excludedId &&
            entry.DeviceType.ToLower() == deviceType.ToLower() &&
            entry.Brand.ToLower() == brand.ToLower() &&
            entry.WorkType.ToLower() == workType.ToLower(),
            cancellationToken);

    private static bool TryNormalize(
        SaveLaborPriceGuideEntryRequest request,
        out string deviceType,
        out string brand,
        out string workType)
    {
        deviceType = request.DeviceType.Trim();
        brand = request.Brand.Trim();
        workType = request.WorkType.Trim();
        return deviceType.Length > 0 && brand.Length > 0 && workType.Length > 0;
    }

    private static LaborPriceGuideEntryResponse ToResponse(LaborPriceGuideEntry entry) =>
        new(entry.Id, entry.DeviceType, entry.Brand, entry.WorkType, entry.Price, entry.IsActive, entry.IsSample);
}
