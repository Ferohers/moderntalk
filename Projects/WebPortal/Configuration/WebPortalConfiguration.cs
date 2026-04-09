using Server;
using Server.Logging;

namespace Server.WebPortal.Configuration;

public static class WebPortalConfiguration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(WebPortalConfiguration));

    public static bool Enabled { get; private set; }
    public static int Port { get; private set; }
    public static string JwtSecret { get; private set; }
    public static int MaxLoginAttemptsPerMinute { get; private set; }
    public static int AccountLockoutMinutes { get; private set; }
    public static int AccessTokenExpiryMinutes { get; private set; }
    public static int RefreshTokenExpiryDays { get; private set; }
    public static string ServerName { get; private set; }
    public static string ConnectionHost { get; private set; }
    public static int ConnectionPort { get; private set; }

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

        if (Enabled)
        {
            logger.Information("Web Portal configured on port {Port}", Port);
        }
    }

    private static string GenerateDefaultJwtSecret()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
