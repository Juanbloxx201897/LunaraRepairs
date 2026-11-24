using Lunara.Api.Data;
using Lunara.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lunara.Api.Services;

// Encapsula las reglas del proceso de reparación para que los controladores no dupliquen transiciones.
public class RepairWorkflowService(LunaraDbContext db)
{
    // Acepta únicamente los siguientes pasos válidos y registra el cambio en el historial.
    public async Task<(bool Success, string Message)> ChangeStatusAsync(
        int id,
        string next,
        string note,
        CancellationToken cancellationToken = default)
    {
        if (!RepairStatuses.All.Contains(next))
            return (false, "El estado indicado no existe.");

        var order = await db.RepairOrders.FirstOrDefaultAsync(
            x => x.Id == id, cancellationToken);

        if (order is null)
            return (false, "No se encontró la orden.");

        // Cada estado define explícitamente a cuáles puede avanzar la orden.
        var allowed = order.Status switch
        {
            RepairStatuses.Received =>
                new[] { RepairStatuses.Diagnosing },

            RepairStatuses.Diagnosing =>
                new[] { RepairStatuses.PendingApproval },

            RepairStatuses.PendingApproval =>
                new[] { RepairStatuses.Approved, RepairStatuses.Rejected },

            RepairStatuses.Approved =>
                new[] { RepairStatuses.Repairing },

            RepairStatuses.Repairing =>
                new[] { RepairStatuses.Testing },

            RepairStatuses.Testing =>
                new[] { RepairStatuses.Repairing, RepairStatuses.Ready },

            RepairStatuses.Ready =>
                new[] { RepairStatuses.Delivered },

            _ => Array.Empty<string>()
        };

        if (!allowed.Contains(next))
        {
            return (false,
                $"Transición no permitida: {order.Status} → {next}.");
        }

        var previous = order.Status;
        order.Status = next;
        order.UpdatedAt = DateTime.UtcNow;

        db.RepairStatusHistory.Add(new RepairStatusHistory
        {
            RepairOrderId = order.Id,
            PreviousStatus = previous,
            NewStatus = next,
            Note = note ?? "",
            ChangedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        return (true, "Estado actualizado.");
    }
}
