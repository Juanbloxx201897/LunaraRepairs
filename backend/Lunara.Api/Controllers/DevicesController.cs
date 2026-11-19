using Lunara.Api.Data;
using Lunara.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Administrator")]
// Mantiene los equipos recibidos y los relaciona con el cliente que los registró.
public class DevicesController(LunaraDbContext db) : ControllerBase
{
    // Incluye el cliente para que la interfaz pueda mostrar sus datos junto al equipo.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Device>>> Get() =>
        Ok(await db.Devices.AsNoTracking().Include(x => x.Client).OrderByDescending(x => x.Id).ToListAsync());

    // Comprueba los campos obligatorios y la existencia del cliente antes de crear el equipo.
    [HttpPost]
    public async Task<ActionResult<Device>> Create(DeviceCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Type) || string.IsNullOrWhiteSpace(dto.Brand) ||
            string.IsNullOrWhiteSpace(dto.Model) || string.IsNullOrWhiteSpace(dto.SerialNumber) ||
            string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Tipo, marca, modelo, número de serie y descripción son obligatorios." });
        if (dto.ClientId <= 0)
            return BadRequest(new { message = "Selecciona un cliente válido." });
        if (!await db.Clients.AnyAsync(x => x.Id == dto.ClientId))
            return BadRequest(new { message = "El cliente indicado no existe." });
        var device = new Device { ClientId = dto.ClientId, Type = dto.Type.Trim(), Brand = dto.Brand.Trim(),
            Model = dto.Model.Trim(), SerialNumber = dto.SerialNumber?.Trim() ?? "", Description = dto.Description?.Trim() ?? "" };
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        return Created($"/api/devices/{device.Id}", device);
    }
}
