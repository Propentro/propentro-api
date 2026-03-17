using Propentro.Application.Common.Interfaces.Email;

namespace Propentro.Infrastructure.Email;

public sealed class EmailTemplateService : IEmailTemplateService
{
    public EmailMessage MemberDeactivated(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Your membership in {organizationName} has been deactivated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>Your membership in <strong>{organizationName}</strong> has been deactivated. You will no longer have access to this organization's resources.</p>
                <p>If you believe this was a mistake, please contact your organization administrator.</p>
                """);

    public EmailMessage MemberReactivated(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Your membership in {organizationName} has been reactivated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>Your membership in <strong>{organizationName}</strong> has been reactivated. You can now access this organization's resources again.</p>
                """);

    public EmailMessage MemberRemoved(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"You have been removed from {organizationName}",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>You have been removed from <strong>{organizationName}</strong>. You will no longer have access to this organization's resources.</p>
                <p>If you believe this was a mistake, please contact the organization administrator.</p>
                """);

    public EmailMessage MemberRolesUpdated(string recipientEmail, string recipientName, string organizationName, IReadOnlyList<string> roleNames) =>
        new(
            To: recipientEmail,
            Subject: $"Your roles in {organizationName} have been updated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>Your roles in <strong>{organizationName}</strong> have been updated. Your current roles are:</p>
                <ul>{string.Join("", roleNames.Select(r => $"<li>{r}</li>"))}</ul>
                """);

    public EmailMessage InviteAccepted(string recipientEmail, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Welcome to {organizationName}",
            HtmlBody: $"""
                <p>Welcome!</p>
                <p>Your invitation to join <strong>{organizationName}</strong> has been accepted. You now have access to the organization's resources.</p>
                """);

    public EmailMessage InviteCanceled(string recipientEmail, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Your invitation to {organizationName} has been canceled",
            HtmlBody: $"""
                <p>The invitation for you to join <strong>{organizationName}</strong> has been canceled.</p>
                <p>If you have questions, please contact the organization administrator.</p>
                """);

    public EmailMessage OrganizationDeactivated(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"{organizationName} has been deactivated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>The organization <strong>{organizationName}</strong> has been deactivated. All members have lost access to this organization's resources.</p>
                <p>If you have questions, please contact your system administrator.</p>
                """);
}
