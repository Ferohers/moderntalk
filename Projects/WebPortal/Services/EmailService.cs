using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Server.Logging;
using Server.WebPortal.Configuration;

namespace Server.WebPortal.Services;

public class EmailService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(EmailService));

    public bool IsConfigured => WebPortalConfiguration.SmtpEnabled
        && !string.IsNullOrWhiteSpace(WebPortalConfiguration.SmtpHost)
        && !string.IsNullOrWhiteSpace(WebPortalConfiguration.SmtpFromAddress);

    public async Task<bool> SendPasswordResetEmail(string toEmail, string username, string resetToken)
    {
        if (!IsConfigured)
        {
            logger.Warning("Web Portal: Cannot send password reset email — SMTP is not configured");
            return false;
        }

        var baseUrl = WebPortalConfiguration.PasswordResetBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            // Fall back to the connection host
            baseUrl = $"http://{WebPortalConfiguration.ConnectionHost}:{WebPortalConfiguration.Port}";
        }

        var resetUrl = $"{baseUrl.TrimEnd('/')}/reset-password.html?token={Uri.EscapeDataString(resetToken)}";

        var subject = $"{WebPortalConfiguration.ServerName} — Password Reset";
        var body = $@"
Hello {username},

A password reset was requested for your account on {WebPortalConfiguration.ServerName}.

Click the link below to reset your password. This link will expire in 15 minutes.

{resetUrl}

If you did not request this reset, you can safely ignore this email. Your password will not be changed.

— {WebPortalConfiguration.ServerName}
";

        return await SendEmail(toEmail, subject, body);
    }

    private async Task<bool> SendEmail(string toAddress, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(WebPortalConfiguration.SmtpHost, WebPortalConfiguration.SmtpPort);

            if (WebPortalConfiguration.SmtpUseSsl)
            {
                client.EnableSsl = true;
            }

            if (!string.IsNullOrWhiteSpace(WebPortalConfiguration.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(
                    WebPortalConfiguration.SmtpUsername,
                    WebPortalConfiguration.SmtpPassword
                );
            }

            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            var from = new MailAddress(WebPortalConfiguration.SmtpFromAddress, WebPortalConfiguration.SmtpFromName);
            var to = new MailAddress(toAddress);

            using var message = new MailMessage(from, to)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            await client.SendMailAsync(message);

            logger.Information("Web Portal: Password reset email sent to {Email}", toAddress);
            return true;
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Web Portal: Failed to send email to {Email}", toAddress);
            return false;
        }
    }
}
