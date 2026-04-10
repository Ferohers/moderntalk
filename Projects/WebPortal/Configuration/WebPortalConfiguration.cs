using System;
using System.Security.Cryptography;
using Server;
using Server.Logging;

namespace Server.WebPortal.Configuration;

public static class WebPortalConfiguration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(WebPortalConfiguration));

    public static bool Enabled { get; private set; }
    public static int Port { get; private set; }
    public static string JwtSecret { get; private set; } = "";
    public static int MaxLoginAttemptsPerMinute { get; private set; }
    public static int AccountLockoutMinutes { get; private set; }
    public static int AccessTokenExpiryMinutes { get; private set; }
    public static int RefreshTokenExpiryDays { get; private set; }
    public static string ServerName { get; private set; } = "";
    public static string ConnectionHost { get; private set; } = "";
    public static int ConnectionPort { get; private set; }

    // SMTP configuration for password reset emails
    public static bool SmtpEnabled { get; private set; }
    public static string SmtpHost { get; private set; } = "";
    public static int SmtpPort { get; private set; }
    public static string SmtpUsername { get; private set; } = "";
    public static string SmtpPassword { get; private set; } = "";
    public static bool SmtpUseSsl { get; private set; }
    public static string SmtpFromAddress { get; private set; } = "";
    public static string SmtpFromName { get; private set; } = "";
    public static string PasswordResetBaseUrl { get; private set; } = "";

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("webPortal.enabled", true);
        Port = ServerConfiguration.GetOrUpdateSetting("webPortal.port", 8080);
        JwtSecret = ServerConfiguration.GetOrUpdateSetting(
            "webPortal.jwtSecret",
            GenerateDefaultJwtSecret()
        );
        MaxLoginAttemptsPerMinute = ServerConfiguration.GetOrUpdateSetting(
            "webPortal.maxLoginAttemptsPerMinute",
            5
        );
        AccountLockoutMinutes = ServerConfiguration.GetOrUpdateSetting(
            "webPortal.accountLockoutMinutes",
            15
        );
        AccessTokenExpiryMinutes = ServerConfiguration.GetOrUpdateSetting(
            "webPortal.accessTokenExpiryMinutes",
            15
        );
        RefreshTokenExpiryDays = ServerConfiguration.GetOrUpdateSetting(
            "webPortal.refreshTokenExpiryDays",
            7
        );
        ServerName = ServerConfiguration.GetOrUpdateSetting("server.name", "ModernUO");
        ConnectionHost = ServerConfiguration.GetOrUpdateSetting("webPortal.connectionHost", "localhost");
        ConnectionPort = ServerConfiguration.GetOrUpdateSetting("webPortal.connectionPort", 2593);

        // SMTP settings
        SmtpEnabled = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.enabled", false);
        SmtpHost = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.host", "");
        SmtpPort = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.port", 587);
        SmtpUsername = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.username", "");
        SmtpPassword = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.password", "");
        SmtpUseSsl = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.useSsl", true);
        SmtpFromAddress = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.fromAddress", "");
        SmtpFromName = ServerConfiguration.GetOrUpdateSetting("webPortal.smtp.fromName", "ModernUO");
        PasswordResetBaseUrl = ServerConfiguration.GetOrUpdateSetting("webPortal.passwordResetBaseUrl", "");

        if (Enabled)
        {
            logger.Information("Web Portal configured on port {Port}", Port);

            if (SmtpEnabled)
            {
                logger.Information("Web Portal SMTP configured: {Host}:{Port} (SSL: {Ssl})", SmtpHost, SmtpPort, SmtpUseSsl);
            }
            else
            {
                logger.Information("Web Portal SMTP is disabled — password reset emails will not be sent");
            }
        }
    }

    private static string GenerateDefaultJwtSecret()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
