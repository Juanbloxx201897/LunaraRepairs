using System.Net;
using System.Net.Mail;
using System.Text;
using Lunara.Api.Models;

namespace Lunara.Api.Services;

public interface IRepairReadyEmailService
{
    Task<BudgetEmailResult> SendAsync(
        RepairOrder order,
        CancellationToken cancellationToken);
}

public sealed class RepairReadyEmailService(
    IConfiguration configuration,
    ILogger<RepairReadyEmailService> logger) : IRepairReadyEmailService
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
            return new(false, "El equipo quedó listo, pero el correo no está configurado. Configura Email:Smtp en el servidor.");

        var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (order.Client is null || !emailValidator.IsValid(order.Client.Email))
            return new(false, "El equipo quedó listo, pero el cliente no tiene un correo válido.");
        if (!emailValidator.IsValid(fromAddress))
            return new(false, "La dirección de envío de Gmail no es válida; verifica Email:Smtp:FromAddress.");

        var port = configuration.GetValue("Email:Smtp:Port", 587);
        if (port is < 1 or > 65535)
            return new(false, "El puerto SMTP configurado no es válido.");

        var senderName = configuration["Email:Smtp:FromName"] ?? "Lunara Repairs";
        var enableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true);
        var deviceName = $"{order.Device?.Brand} {order.Device?.Model}".Trim();
        var safeCode = WebUtility.HtmlEncode(order.Code);
        var safeDevice = WebUtility.HtmlEncode(deviceName);
        var safeDiagnosis = WebUtility.HtmlEncode(order.Diagnosis);
        var safeWork = WebUtility.HtmlEncode(order.WorkPerformed);
        var budget = (order.Budget ?? 0m).ToString(
            "C0",
            System.Globalization.CultureInfo.GetCultureInfo("es-CO"));
        var body = $"""
            <h2>Tu equipo está listo para recoger</h2>
            <p>Hola {WebUtility.HtmlEncode(order.Client.FullName)},</p>
            <p>El equipo {safeDevice} de la orden {safeCode} está listo para recoger.</p>
            <p><strong>Diagnóstico:</strong> {safeDiagnosis}</p>
            <p><strong>Trabajo realizado:</strong> {safeWork}</p>
            <p><strong>Presupuesto aprobado:</strong> {budget}</p>
            <p>Te esperamos para entregarte el equipo. Si tienes preguntas, responde a este correo.</p>
            """;
        var plainTextBody = $"""
            Tu equipo está listo para recoger

            Hola {order.Client.FullName},
            El equipo {deviceName} de la orden {order.Code} está listo para recoger.

            Diagnóstico: {order.Diagnosis}
            Trabajo realizado: {order.WorkPerformed}
            Presupuesto aprobado: {budget}

            Te esperamos para entregarte el equipo. Si tienes preguntas, responde a este correo.
            """;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, senderName),
            Subject = $"Tu equipo está listo para recoger - {order.Code}",
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
                "El servidor SMTP aceptó el aviso de equipo listo para la orden {RepairCode}.",
                order.Code);
            return new(
                true,
                $"El equipo quedó listo y Gmail aceptó el aviso para enviarlo a {order.Client.Email}. Revisa también Spam y correo no deseado.");
        }
        catch (SmtpException exception)
        {
            logger.LogError(
                exception,
                "No se pudo enviar el aviso de equipo listo para la orden {RepairCode}.",
                order.Code);
            return new(
                false,
                "El equipo quedó listo, pero no se pudo enviar el correo. Verifica la configuración SMTP o vuelve a intentar.");
        }
    }
}
