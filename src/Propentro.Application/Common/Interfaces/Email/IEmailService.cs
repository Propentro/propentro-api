using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Interfaces.Email;

public interface IEmailService
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);
