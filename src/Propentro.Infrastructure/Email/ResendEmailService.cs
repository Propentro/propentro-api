using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Infrastructure.Email;

public sealed class ResendEmailService(
    HttpClient httpClient,
    IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            Log.Warning("Email API key is not configured — skipping email to {To}", message.To);
            return Result.Success();
        }

        try
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var payload = new
            {
                from = string.IsNullOrWhiteSpace(_options.FromName)
                    ? _options.FromEmail
                    : $"{_options.FromName} <{_options.FromEmail}>",
                to = new[] { message.To },
                subject = message.Subject,
                html = message.HtmlBody,
                text = message.TextBody
            };

            var response = await httpClient.PostAsJsonAsync("/emails", payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Email sent successfully to {To}, subject: {Subject}", message.To, message.Subject);
                return Result.Success();
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Error(
                "Resend API returned {StatusCode} for email to {To}: {ErrorBody}",
                (int)response.StatusCode,
                message.To,
                errorBody);

            return new Error("Email.SendFailed", $"Email provider returned {(int)response.StatusCode}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to send email to {To}", message.To);
            return new Error("Email.SendFailed", "An unexpected error occurred while sending email");
        }
    }
}
