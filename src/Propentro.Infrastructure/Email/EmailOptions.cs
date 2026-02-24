namespace Propentro.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string ApiKey { get; init; } = string.Empty;

    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;
}
