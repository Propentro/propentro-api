namespace Propentro.Application.Common.Email;

public static class EmailTemplates
{
    public static EmailContent WelcomeEmail(string firstName)
    {
        var subject = "Welcome to Propentro";

        var body = $"""
            <h1 style="margin:0 0 16px;font-size:24px;font-weight:700;color:#18181b;">Welcome to Propentro</h1>
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;color:#3f3f46;">
                Hi {firstName},
            </p>
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;color:#3f3f46;">
                Your account has been created. You can now sign in and start managing your properties.
            </p>
            <p style="margin:0;font-size:16px;line-height:1.6;color:#3f3f46;">
                If you have any questions, feel free to reach out to our support team.
            </p>
            """;

        var text = $"""
            Welcome to Propentro

            Hi {firstName},

            Your account has been created. You can now sign in and start managing your properties.

            If you have any questions, feel free to reach out to our support team.
            """;

        return new EmailContent(subject, WrapInLayout(subject, body), text);
    }

    public static EmailContent MembershipAddedEmail(string firstName, string orgName)
    {
        var subject = $"You've been added to {orgName}";

        var body = $"""
            <h1 style="margin:0 0 16px;font-size:24px;font-weight:700;color:#18181b;">You've been added to an organization</h1>
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;color:#3f3f46;">
                Hi {firstName},
            </p>
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;color:#3f3f46;">
                You have been added to <strong>{orgName}</strong> on Propentro. You can now access the organization's properties and data.
            </p>
            <p style="margin:0;font-size:16px;line-height:1.6;color:#3f3f46;">
                Sign in to get started.
            </p>
            """;

        var text = $"""
            You've been added to an organization

            Hi {firstName},

            You have been added to {orgName} on Propentro. You can now access the organization's properties and data.

            Sign in to get started.
            """;

        return new EmailContent(subject, WrapInLayout(subject, body), text);
    }

    public static EmailContent MembershipRemovedEmail(string firstName, string orgName)
    {
        var subject = $"You've been removed from {orgName}";

        var body = $"""
            <h1 style="margin:0 0 16px;font-size:24px;font-weight:700;color:#18181b;">Organization access removed</h1>
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;color:#3f3f46;">
                Hi {firstName},
            </p>
            <p style="margin:0 0 12px;font-size:16px;line-height:1.6;color:#3f3f46;">
                Your access to <strong>{orgName}</strong> on Propentro has been removed. You will no longer be able to access the organization's data.
            </p>
            <p style="margin:0;font-size:16px;line-height:1.6;color:#3f3f46;">
                If you believe this was a mistake, please contact your organization administrator.
            </p>
            """;

        var text = $"""
            Organization access removed

            Hi {firstName},

            Your access to {orgName} on Propentro has been removed. You will no longer be able to access the organization's data.

            If you believe this was a mistake, please contact your organization administrator.
            """;

        return new EmailContent(subject, WrapInLayout(subject, body), text);
    }

    private static string WrapInLayout(string title, string bodyContent) =>
        $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>{title}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f4f5;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f5;">
                <tr>
                    <td align="center" style="padding:40px 16px;">
                        <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">
                            <!-- Logo -->
                            <tr>
                                <td style="padding:0 0 24px;text-align:center;">
                                    <span style="font-size:20px;font-weight:700;color:#18181b;letter-spacing:-0.5px;">Propentro</span>
                                </td>
                            </tr>
                            <!-- Card -->
                            <tr>
                                <td style="background-color:#ffffff;border-radius:8px;padding:32px;">
                                    {bodyContent}
                                </td>
                            </tr>
                            <!-- Footer -->
                            <tr>
                                <td style="padding:24px 0 0;text-align:center;">
                                    <p style="margin:0;font-size:13px;color:#a1a1aa;">
                                        Propentro &mdash; Property Management
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
}

public sealed record EmailContent(string Subject, string HtmlBody, string TextBody);
