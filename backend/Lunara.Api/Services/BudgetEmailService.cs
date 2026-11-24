using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Lunara.Api.Models;

namespace Lunara.Api.Services;

public sealed record BudgetEmailResult(bool Sent, string Message);

public interface IBudgetEmailService
{
    Task<BudgetEmailResult> SendAsync(RepairOrder order, CancellationToken cancellationToken);
}

public sealed class BudgetEmailService(
    IConfiguration configuration,
    ILogger<BudgetEmailService> logger) : IBudgetEmailService
{
    public async Task<BudgetEmailResult> SendAsync(
        RepairOrder order,
        CancellationToken cancellationToken)
    {
        var host = configuration["Email:Smtp:Host"];
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var fromAddress = configuration["Email:Smtp:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromAddress))
            return new(false, "El presupuesto se guardó, pero el correo no está configurado. Configura Email:Smtp en el servidor.");

        var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (order.Client is null || !emailValidator.IsValid(order.Client.Email))
            return new(false, "El presupuesto se guardó, pero el cliente no tiene un correo válido.");
        if (!emailValidator.IsValid(fromAddress))
            return new(false, "La dirección de envío de Gmail no es válida; verifica Email:Smtp:FromAddress.");

        var senderName = configuration["Email:Smtp:FromName"] ?? "Lunara Repairs";
        var port = configuration.GetValue("Email:Smtp:Port", 587);
        var enableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true);
        if (port is < 1 or > 65535)
            return new(false, "El puerto SMTP configurado no es válido.");
        var labor = order.LaborCost.ToString("C0", CultureInfo.GetCultureInfo("es-CO"));
        var total = (order.Budget ?? 0m).ToString("C0", CultureInfo.GetCultureInfo("es-CO"));
        var safeOrder = WebUtility.HtmlEncode(order.Code);
        var safeDevice = WebUtility.HtmlEncode(
            $"{order.Device?.Brand} {order.Device?.Model}".Trim());
        var safeDiagnosis = WebUtility.HtmlEncode(order.Diagnosis);
        var safeWorkPerformed = WebUtility.HtmlEncode(order.WorkPerformed);
        BudgetEmailLine[] parts;
        try
        {
            parts = JsonSerializer.Deserialize<BudgetEmailLine[]>(order.BudgetPartsJson) ?? [];
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "El presupuesto {RepairCode} tiene un detalle de repuestos inválido.", order.Code);
            return new(false, "El detalle de repuestos del presupuesto no es válido; vuelve a calcularlo antes de enviarlo.");
        }
        var rows = string.Join("", parts.Select(part =>
                $"<tr><td>{WebUtility.HtmlEncode(part.Name)}</td>" +
                $"<td>{part.Quantity}</td><td>{part.UnitPrice.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))}</td>" +
                $"<td>{(part.Quantity * part.UnitPrice).ToString("C0", CultureInfo.GetCultureInfo("es-CO"))}</td></tr>"));
        var plainTextParts = parts.Length == 0
            ? "Sin repuestos"
            : string.Join(Environment.NewLine, parts.Select(part =>
                $"- {part.Name} x {part.Quantity}: " +
                $"{(part.Quantity * part.UnitPrice).ToString("C0", CultureInfo.GetCultureInfo("es-CO"))}"));
        var body = $"""
            <h2>Presupuesto de reparación {safeOrder}</h2>
            <p>Equipo: {safeDevice}</p>
            <p>Diagnóstico: {safeDiagnosis}</p>
            <p>Trabajo realizado: {safeWorkPerformed}</p>
            <table border="1" cellpadding="6" cellspacing="0">
              <thead><tr><th>Concepto</th><th>Cantidad</th><th>Precio unitario</th><th>Subtotal</th></tr></thead>
              <tbody>{rows}<tr><td colspan="3">Mano de obra ({WebUtility.HtmlEncode(order.LaborWorkType)})</td><td>{labor}</td></tr></tbody>
              <tfoot><tr><th colspan="3">Total</th><th>{total}</th></tr></tfoot>
            </table>
            <p>Ingresa al portal de Lunara Repairs para revisar y responder a este presupuesto.</p>
            """;
        var plainTextBody = $"""
            Presupuesto de reparación {order.Code}

            Equipo: {order.Device?.Brand} {order.Device?.Model}
            Diagnóstico: {order.Diagnosis}
            Trabajo realizado: {order.WorkPerformed}

            Repuestos:
            {plainTextParts}

            Mano de obra ({order.LaborWorkType}): {labor}
            Total: {total}

            Ingresa al portal de Lunara Repairs para revisar y responder a este presupuesto.
            """;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, senderName),
            Subject = $"Presupuesto de reparación {order.Code}",
            Body = plainTextBody,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            body,
            Encoding.UTF8,
            "text/html"));
        message.To.Add(new MailAddress(order.Client.Email.Trim(), order.Client.FullName));
        message.ReplyToList.Add(new MailAddress(fromAddress, senderName));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation(
                "El servidor SMTP aceptó el presupuesto {RepairCode} para su entrega.",
                order.Code);
            return new(
                true,
                $"Gmail aceptó el presupuesto para enviarlo a {order.Client.Email}. " +
                "La entrega final depende del proveedor del destinatario; revisa también Spam y correo no deseado.");
        }
        catch (SmtpException exception)
        {
            logger.LogError(exception, "No se pudo enviar el presupuesto {RepairCode} por SMTP.", order.Code);
            return new(false, "El presupuesto se guardó, pero no se pudo enviar el correo. Verifica la configuración SMTP y vuelve a intentar.");
        }
    }

    private sealed record BudgetEmailLine(string Name, int Quantity, decimal UnitPrice);
}
