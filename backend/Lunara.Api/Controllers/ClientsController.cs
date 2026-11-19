using Lunara.Api.Data;
using Lunara.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lunara.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// Expone las operaciones de clientes y delega la persistencia en el contexto de datos.
public class ClientsController(LunaraDbContext db) : ControllerBase
{
    // Permite buscar por cédula exacta o filtrar el directorio por nombre, correo o cédula.
    [HttpGet]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<ActionResult<IEnumerable<Client>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? cedula)
    {
        var query = db.Clients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(cedula))
        {
            var normalizedCedula = NormalizeCedula(cedula);
            if (normalizedCedula is null)
                return BadRequest(new { message = "La cédula debe contener solo números." });
            query = query.Where(x => x.Cedula == normalizedCedula);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var cedulaCandidate = term.Replace(".", "").Replace("-", "").Replace(" ", "");
            var cedulaTerm = cedulaCandidate.Length > 0 &&
                             cedulaCandidate.All(character => character is >= '0' and <= '9')
                ? cedulaCandidate
                : term;
            query = query.Where(x => x.Cedula.Contains(cedulaTerm) ||
                                     x.FullName.Contains(term) ||
                                     x.Email.Contains(term));
        }

        return Ok(await query.OrderBy(x => x.FullName).ToListAsync());
    }

    // Busca por identificador y responde 404 cuando el cliente no existe.
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<ActionResult<Client>> GetById(int id)
    {
        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return client is null ? NotFound() : Ok(client);
    }

    // Devuelve las órdenes del cliente en orden cronológico para consultar su historial completo.
    [HttpGet("{id:int}/repairs")]
    public async Task<ActionResult<IEnumerable<ClientRepairHistoryDto>>> GetRepairHistory(int id)
    {
        var clientId = User.FindFirstValue("clientId");
        if (!User.IsInRole("Admin") && !User.IsInRole("Administrator") &&
            (!int.TryParse(clientId, out var linkedClientId) || linkedClientId != id))
            return Forbid();

        if (!await db.Clients.AnyAsync(x => x.Id == id))
            return NotFound();

        var history = await db.RepairOrders.AsNoTracking()
            .Where(x => x.ClientId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ClientRepairHistoryDto(
                x.Id,
                x.Code,
                x.Device!.Brand + " " + x.Device.Model,
                x.ProblemDescription,
                x.Status,
                x.CreatedAt))
            .ToListAsync();

        return Ok(history);
    }

    private static string? NormalizeCedula(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace(".", "").Replace("-", "").Replace(" ", "");
        return normalized.Length is >= 5 and <= 20 &&
               normalized.All(character => character is >= '0' and <= '9')
            ? normalized
            : null;
    }
}
