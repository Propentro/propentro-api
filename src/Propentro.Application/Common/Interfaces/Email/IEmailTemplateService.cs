namespace Propentro.Application.Common.Interfaces.Email;

public interface IEmailTemplateService
{
    EmailMessage MemberDeactivated(string recipientEmail, string recipientName, string organizationName);
    EmailMessage MemberReactivated(string recipientEmail, string recipientName, string organizationName);
    EmailMessage MemberRemoved(string recipientEmail, string recipientName, string organizationName);
    EmailMessage MemberRolesUpdated(string recipientEmail, string recipientName, string organizationName, IReadOnlyList<string> roleNames);
    EmailMessage InviteAccepted(string recipientEmail, string organizationName);
    EmailMessage InviteCanceled(string recipientEmail, string organizationName);
    EmailMessage OrganizationDeactivated(string recipientEmail, string recipientName, string organizationName);
}
