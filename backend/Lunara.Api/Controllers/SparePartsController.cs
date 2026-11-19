using Lunara.Api.Data;
using Lunara.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Administrator")]
// Expone el inventario; el servidor genera el código para que cada repuesto tenga una identificación única.
public class SparePartsController(LunaraDbContext db) : ControllerBase
{
    // Devuelve el inventario y permite una búsqueda sencilla por los campos principales.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SparePart>>> Get([FromQuery] string? search)
    {
        var query = db.SpareParts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term) ||
                                     x.Category.Contains(term) || x.Brand.Contains(term));
        }

        return Ok(await query.OrderBy(x => x.Name).ToListAsync());
    }

    // Valida los datos del formulario y asigna el código antes de guardar el repuesto.
    [HttpPost]
    public async Task<ActionResult<SparePart>> Create(SparePartCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Category) ||
            string.IsNullOrWhiteSpace(dto.Brand) || string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Nombre, categoría, marca y descripción son obligatorios." });
        if (!dto.Quantity.HasValue || !dto.UnitPrice.HasValue)
            return BadRequest(new { message = "La cantidad y el precio unitario son obligatorios." });
        if (dto.Quantity < 0 || dto.UnitPrice < 0)
            return BadRequest(new { message = "La cantidad y el precio no pueden ser negativos." });

        var sparePart = new SparePart
        {
            Code = $"REP-{Guid.NewGuid():N}".ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            Brand = dto.Brand.Trim(),
            Description = dto.Description?.Trim() ?? "",
            Quantity = dto.Quantity.Value,
            UnitPrice = dto.UnitPrice.Value
        };

        db.SpareParts.Add(sparePart);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { search = sparePart.Code }, sparePart);
    }

    [HttpGet("purchases")]
    public async Task<ActionResult<IEnumerable<SparePartPurchase>>> GetPurchases() =>
        Ok(await db.SparePartPurchases.AsNoTracking()
            .OrderBy(x => x.Status == "Pedido" ? 0 : 1)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync());

    [HttpPost("purchases")]
    public async Task<ActionResult<SparePartPurchase>> CreatePurchase(SparePartPurchaseCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Supplier) || dto.Quantity <= 0 || dto.UnitPrice < 0)
            return BadRequest(new { message = "Proveedor, cantidad mayor a cero y precio no negativo son obligatorios." });

        SparePart? part = null;
        if (dto.SparePartId.HasValue)
        {
            part = await db.SpareParts.FirstOrDefaultAsync(x => x.Id == dto.SparePartId);
            if (part is null)
                return BadRequest(new { message = "El repuesto seleccionado ya no existe en el inventario." });
        }
        else if (string.IsNullOrWhiteSpace(dto.Name) ||
                 string.IsNullOrWhiteSpace(dto.Category) ||
                 string.IsNullOrWhiteSpace(dto.Brand))
        {
            return BadRequest(new { message = "Para pedir un repuesto nuevo indica nombre, categoría y marca." });
        }

        var purchase = new SparePartPurchase
        {
            SparePartId = part?.Id,
            Name = part?.Name ?? dto.Name.Trim(),
            Category = part?.Category ?? dto.Category.Trim(),
            Brand = part?.Brand ?? dto.Brand.Trim(),
            Description = part?.Description ?? dto.Description.Trim(),
            Supplier = dto.Supplier.Trim(),
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            Status = "Pedido"
        };
        db.SparePartPurchases.Add(purchase);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPurchases), new { id = purchase.Id }, purchase);
    }

    [HttpPut("purchases/{id:int}/purchased")]
    public async Task<IActionResult> MarkPurchased(int id)
    {
        var purchase = await db.SparePartPurchases.FirstOrDefaultAsync(x => x.Id == id);
        if (purchase is null) return NotFound(new { message = "No se encontró el pedido." });
        if (purchase.Status == "Comprado")
            return Conflict(new { message = "Este pedido ya fue marcado como comprado." });

        var part = purchase.SparePartId.HasValue
            ? await db.SpareParts.FirstOrDefaultAsync(x => x.Id == purchase.SparePartId.Value)
            : null;
        if (part is null)
        {
            part = new SparePart
            {
                Code = $"REP-{Guid.NewGuid():N}".ToUpperInvariant(),
                Name = purchase.Name,
                Category = purchase.Category,
                Brand = purchase.Brand,
                Description = purchase.Description,
                Quantity = 0,
                UnitPrice = purchase.UnitPrice
            };
            db.SpareParts.Add(part);
            await db.SaveChangesAsync();
            purchase.SparePartId = part.Id;
        }

        part.Quantity += purchase.Quantity;
        part.UnitPrice = purchase.UnitPrice;
        purchase.Status = "Comprado";
        purchase.PurchasedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
