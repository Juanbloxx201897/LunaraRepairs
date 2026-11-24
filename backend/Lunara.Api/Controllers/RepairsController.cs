using Lunara.Api.Data;
using Lunara.Api.Models;
using Lunara.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Lunara.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// Atiende las solicitudes HTTP de reparaciones; las reglas de transición viven en RepairWorkflowService.
public class RepairsController(
    LunaraDbContext db,
    RepairWorkflowService workflow,
    IBudgetEmailService budgetEmailService,
    IRepairReadyEmailService repairReadyEmailService) : ControllerBase
{
    // Permite filtrar por estado o buscar órdenes y carga los datos que la pantalla necesita mostrar.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairOrder>>> Get([FromQuery] string? status, [FromQuery] string? search)
    {
        var query = db.RepairOrders.AsNoTracking()
            .Include(x => x.Client).Include(x => x.Device).Include(x => x.Technician).AsQueryable();
        if (User.IsInRole("Technician"))
            query = query.Where(x => x.TechnicianId == UserIntClaim("technicianId"));
        else if (User.IsInRole("Client"))
            query = query.Where(x => x.ClientId == UserIntClaim("clientId"));
        else if (!IsAdmin())
            return Forbid();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Code.Contains(search) || x.Client!.FullName.Contains(search) ||
                                     x.Device!.Brand.Contains(search) || x.Device.Model.Contains(search));
        return Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    // Recupera el detalle de una orden junto con su historial de cambios.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairOrder>> GetById(int id)
    {
        var order = await db.RepairOrders.AsNoTracking().Include(x => x.Client).Include(x => x.Device)
            .Include(x => x.Technician).Include(x => x.History.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (order is not null && !CanAccess(order))
            return Forbid();
        return order is null ? NotFound() : Ok(order);
    }

    // Valida las relaciones y crea la orden con su primer evento de historial.
    [HttpPost]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<ActionResult<RepairOrder>> Create(RepairCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ProblemDescription))
            return BadRequest(new { message = "La descripción del problema es obligatoria." });

        var device = await db.Devices.FirstOrDefaultAsync(x => x.Id == dto.DeviceId && x.ClientId == dto.ClientId);
        if (device is null) return BadRequest(new { message = "El dispositivo no pertenece al cliente seleccionado." });
        if (!await db.Clients.AnyAsync(x => x.Id == dto.ClientId))
            return BadRequest(new { message = "El cliente no existe." });
        if (dto.TechnicianId.HasValue && !await db.Technicians.AnyAsync(x => x.Id == dto.TechnicianId && x.Active))
            return BadRequest(new { message = "El técnico no existe o está inactivo." });

        var order = new RepairOrder
        {
            Code = $"LR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            ClientId = dto.ClientId, DeviceId = dto.DeviceId, TechnicianId = dto.TechnicianId,
            ProblemDescription = dto.ProblemDescription.Trim(), Status = RepairStatuses.Received
        };
        db.RepairOrders.Add(order);
        await db.SaveChangesAsync();
        db.RepairStatusHistory.Add(new RepairStatusHistory
        {
            RepairOrderId = order.Id, NewStatus = order.Status, Note = "Orden recibida", ChangedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // Registra en una sola operación cliente (si es nuevo), equipo, orden y primer evento del historial.
    [HttpPost("register")]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<ActionResult<RepairOrder>> Register(RepairRegistrationDto dto)
    {
        var cedula = NormalizeCedula(dto.Cedula);
        if (cedula is null)
            return BadRequest(new { message = "La cédula debe contener solo números." });
        if (string.IsNullOrWhiteSpace(dto.Type) || string.IsNullOrWhiteSpace(dto.Brand) ||
            string.IsNullOrWhiteSpace(dto.Model) || string.IsNullOrWhiteSpace(dto.SerialNumber) ||
            string.IsNullOrWhiteSpace(dto.DeviceDescription) ||
            string.IsNullOrWhiteSpace(dto.ProblemDescription))
            return BadRequest(new { message = "Completa tipo, marca, modelo, serial, descripción del equipo y problema reportado." });
        if (dto.TechnicianId.HasValue &&
            !await db.Technicians.AnyAsync(x => x.Id == dto.TechnicianId && x.Active))
            return BadRequest(new { message = "El técnico no existe o está inactivo." });

        var client = await db.Clients.FirstOrDefaultAsync(x => x.Cedula == cedula);
        if (client is null)
        {
            var phone = ClientPhone.Normalize(dto.Phone);
            if (string.IsNullOrWhiteSpace(dto.FullName) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(dto.Email) ||
                phone is null)
                return BadRequest(new { message = "Para un cliente nuevo son obligatorios cédula, nombre, correo válido y teléfono con indicativo internacional (por ejemplo, +57)." });

            client = new Client
            {
                Cedula = cedula,
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                Phone = phone
            };
        }

        var device = new Device
        {
            Client = client,
            Type = dto.Type.Trim(),
            Brand = dto.Brand.Trim(),
            Model = dto.Model.Trim(),
            SerialNumber = dto.SerialNumber.Trim(),
            Description = dto.DeviceDescription.Trim()
        };
        var order = new RepairOrder
        {
            Client = client,
            Device = device,
            TechnicianId = dto.TechnicianId,
            ProblemDescription = dto.ProblemDescription.Trim(),
            Code = $"LR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            Status = RepairStatuses.Received
        };
        order.History.Add(new RepairStatusHistory
        {
            NewStatus = order.Status,
            Note = "Orden recibida",
            ChangedAt = DateTime.UtcNow
        });

        db.RepairOrders.Add(order);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}/technician")]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<IActionResult> AssignTechnician(int id, RepairAssignmentDto dto)
    {
        var order = await db.RepairOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (dto.TechnicianId.HasValue &&
            !await db.Technicians.AnyAsync(x => x.Id == dto.TechnicianId && x.Active))
            return BadRequest(new { message = "El técnico no existe o está inactivo." });

        order.TechnicianId = dto.TechnicianId;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Guarda el diagnóstico y presupuesto; luego solicita aprobación mediante el flujo compartido.
    [HttpPut("{id:int}/diagnosis")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> SaveDiagnosis(
        int id,
        DiagnosisDto dto,
        CancellationToken cancellationToken)
    {
        var order = await db.RepairOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!IsAssignedTechnician(order)) return Forbid();
        if (order.Status is not (RepairStatuses.Received or RepairStatuses.Diagnosing))
            return BadRequest(new { message = "Solo se puede registrar el diagnóstico antes de solicitar aprobación." });
        if (string.IsNullOrWhiteSpace(dto.Diagnosis) ||
            dto.WorkType is { Length: > 120 } ||
            dto.LaborCost < 0 ||
            dto.Budget < 0 ||
            dto.Parts is { Count: > 30 } ||
            dto.Parts?.Any(part => part.Quantity is < 1 or > 1000) == true)
            return BadRequest(new { message = "El diagnóstico es obligatorio y los importes y cantidades del presupuesto deben ser válidos." });

        var budgetLines = new List<BudgetPartLine>();
        if (dto.Parts is { Count: > 0 })
        {
            if (dto.Parts.Select(part => part.SparePartId).Distinct().Count() != dto.Parts.Count)
                return BadRequest(new { message = "No repitas un repuesto; ajusta su cantidad en una sola línea." });
            var partIds = dto.Parts.Select(part => part.SparePartId).ToArray();
            var catalog = await db.SpareParts.AsNoTracking()
                .Where(part => partIds.Contains(part.Id))
                .ToDictionaryAsync(part => part.Id, cancellationToken);
            if (catalog.Count != partIds.Length)
                return BadRequest(new { message = "Uno o más repuestos ya no están disponibles en el catálogo." });

            budgetLines = dto.Parts.Select(line =>
            {
                var part = catalog[line.SparePartId];
                return new BudgetPartLine(part.Name, line.Quantity, part.UnitPrice);
            }).ToList();
        }

        order.Diagnosis = dto.Diagnosis.Trim();
        order.LaborWorkType = string.IsNullOrWhiteSpace(dto.WorkType)
            ? "Trabajo personalizado"
            : dto.WorkType.Trim();
        order.LaborCost = dto.LaborCost;
        order.BudgetPartsJson = JsonSerializer.Serialize(budgetLines);
        order.Budget = dto.Parts is null && dto.LaborCost == 0
            ? dto.Budget
            : dto.LaborCost + budgetLines.Sum(line => line.Quantity * line.UnitPrice);
        if (order.Status == RepairStatuses.Received)
        {
            order.Status = RepairStatuses.Diagnosing;
            db.RepairStatusHistory.Add(new RepairStatusHistory { RepairOrderId = id, PreviousStatus = RepairStatuses.Received,
                NewStatus = RepairStatuses.Diagnosing, Note = "Diagnóstico iniciado", ChangedAt = DateTime.UtcNow });
        }
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var result = await workflow.ChangeStatusAsync(
            id,
            RepairStatuses.PendingApproval,
            dto.Note ?? "Presupuesto enviado para aprobación",
            cancellationToken);
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        var orderForEmail = await db.RepairOrders.AsNoTracking()
            .Include(repair => repair.Client)
            .Include(repair => repair.Device)
            .FirstAsync(repair => repair.Id == id, cancellationToken);
        var emailResult = await budgetEmailService.SendAsync(orderForEmail, cancellationToken);
        return Ok(new BudgetSubmissionResponse(emailResult.Sent, emailResult.Message));
    }

    [HttpPost("{id:int}/budget-email")]
    public async Task<IActionResult> ResendBudgetEmail(int id, CancellationToken cancellationToken)
    {
        var order = await db.RepairOrders.AsNoTracking()
            .Include(repair => repair.Client)
            .Include(repair => repair.Device)
            .FirstOrDefaultAsync(repair => repair.Id == id, cancellationToken);
        if (order is null) return NotFound();
        if (!IsAdmin() && !IsAssignedTechnician(order))
            return Forbid();
        if (order.Status != RepairStatuses.PendingApproval)
            return BadRequest(new { message = "Solo se puede enviar por correo un presupuesto pendiente de aprobación." });

        var result = await budgetEmailService.SendAsync(order, cancellationToken);
        return result.Sent
            ? Ok(new BudgetSubmissionResponse(true, result.Message))
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                new BudgetSubmissionResponse(false, result.Message));
    }

    [HttpPost("{id:int}/ready-email")]
    public async Task<IActionResult> ResendReadyEmail(int id, CancellationToken cancellationToken)
    {
        var order = await db.RepairOrders.AsNoTracking()
            .Include(repair => repair.Client)
            .Include(repair => repair.Device)
            .FirstOrDefaultAsync(repair => repair.Id == id, cancellationToken);
        if (order is null) return NotFound();
        if (!IsAdmin() && !IsAssignedTechnician(order))
            return Forbid();
        if (order.Status != RepairStatuses.Ready)
            return BadRequest(new { message = "Solo se puede enviar el aviso cuando el equipo está listo para recoger." });

        var result = await repairReadyEmailService.SendAsync(order, cancellationToken);
        return result.Sent
            ? Ok(new ReadyNotificationResponse(true, result.Message))
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ReadyNotificationResponse(false, result.Message));
    }

    // Centraliza los cambios de estado para que todos respeten las mismas reglas del negocio.
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int id,
        StatusUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var order = await db.RepairOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (IsAdmin())
        {
            if (order.Status != RepairStatuses.PendingApproval ||
                dto.Status is not (RepairStatuses.Approved or RepairStatuses.Rejected))
                return Forbid();
        }
        else if (User.IsInRole("Client"))
        {
            if (order.ClientId != UserIntClaim("clientId") ||
                order.Status != RepairStatuses.PendingApproval ||
                dto.Status is not (RepairStatuses.Approved or RepairStatuses.Rejected))
                return Forbid();
        }
        else if (User.IsInRole("Technician"))
        {
            if (!IsAssignedTechnician(order) ||
                dto.Status is RepairStatuses.Approved or RepairStatuses.Rejected)
                return Forbid();
        }
        else
        {
            return Forbid();
        }

        var result = await workflow.ChangeStatusAsync(
            id, dto.Status, dto.Note ?? "", cancellationToken);
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        if (dto.Status != RepairStatuses.Ready)
            return NoContent();

        var readyOrder = await db.RepairOrders.AsNoTracking()
            .Include(repair => repair.Client)
            .Include(repair => repair.Device)
            .FirstAsync(repair => repair.Id == id, cancellationToken);
        var emailResult = await repairReadyEmailService.SendAsync(
            readyOrder,
            cancellationToken);
        return Ok(new ReadyNotificationResponse(emailResult.Sent, emailResult.Message));
    }

    // Registra el trabajo técnico solo mientras la orden está en reparación.
    [HttpPut("{id:int}/work")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> SaveWork(int id, WorkPerformedDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.WorkPerformed))
            return BadRequest(new { message = "La descripción del trabajo realizado es obligatoria." });

        var order = await db.RepairOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!IsAssignedTechnician(order)) return Forbid();
        if (order.Status != RepairStatuses.Repairing)
            return BadRequest(new { message = "El trabajo solo se registra cuando la orden está en reparación." });
        order.WorkPerformed = dto.WorkPerformed.Trim();
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Resume las órdenes por estado para alimentar las tarjetas y gráficos del dashboard.
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        var orders = db.RepairOrders.AsNoTracking();
        var now = DateTime.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-5);
        var deliveredSales = await db.RepairStatusHistory.AsNoTracking()
            .Where(history =>
                history.NewStatus == RepairStatuses.Delivered &&
                history.ChangedAt >= firstMonth)
            .Join(db.RepairOrders.AsNoTracking(),
                history => history.RepairOrderId,
                repair => repair.Id,
                (history, repair) => new { history.ChangedAt, repair.Budget })
            .ToListAsync(cancellationToken);
        var monthlySales = Enumerable.Range(0, 6)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month =>
            {
                var sales = deliveredSales.Where(sale =>
                    sale.ChangedAt.Year == month.Year &&
                    sale.ChangedAt.Month == month.Month).ToArray();
                return new
                {
                    month = month.ToString("yyyy-MM"),
                    revenue = sales.Sum(sale => sale.Budget ?? 0m),
                    orders = sales.Length
                };
            })
            .ToArray();
        var deliveredBudgets = await orders
            .Where(x => x.Status == RepairStatuses.Delivered)
            .Select(x => x.Budget)
            .ToListAsync(cancellationToken);
        return Ok(new {
            total = await orders.CountAsync(),
            received = await orders.CountAsync(x => x.Status == RepairStatuses.Received),
            diagnosing = await orders.CountAsync(x => x.Status == RepairStatuses.Diagnosing),
            pendingApproval = await orders.CountAsync(x => x.Status == RepairStatuses.PendingApproval),
            approved = await orders.CountAsync(x => x.Status == RepairStatuses.Approved),
            repairing = await orders.CountAsync(x => x.Status == RepairStatuses.Repairing),
            testing = await orders.CountAsync(x => x.Status == RepairStatuses.Testing),
            ready = await orders.CountAsync(x => x.Status == RepairStatuses.Ready),
            delivered = await orders.CountAsync(x => x.Status == RepairStatuses.Delivered),
            rejected = await orders.CountAsync(x => x.Status == RepairStatuses.Rejected),
            deliveredRevenue = deliveredBudgets.Sum(budget => budget ?? 0m),
            monthlySales
        });
    }

    private bool IsAdmin() =>
        User.IsInRole("Admin") || User.IsInRole("Administrator");

    private int? UserIntClaim(string claimName) =>
        int.TryParse(User.FindFirstValue(claimName), out var value) ? value : null;

    private bool IsAssignedTechnician(RepairOrder order) =>
        User.IsInRole("Technician") &&
        order.TechnicianId == UserIntClaim("technicianId");

    private bool CanAccess(RepairOrder order) =>
        IsAdmin() ||
        IsAssignedTechnician(order) ||
        (User.IsInRole("Client") && order.ClientId == UserIntClaim("clientId"));

    private sealed record BudgetPartLine(string Name, int Quantity, decimal UnitPrice);

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