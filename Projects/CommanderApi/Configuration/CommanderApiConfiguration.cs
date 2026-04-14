using System;
using System.Security.Cryptography;
using Server;
using Server.Logging;

namespace Server.CommanderApi.Configuration;

public static class CommanderApiConfiguration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CommanderApiConfiguration));

    public static bool Enabled { get; private set; }
    public static int Port { get; private set; }
    public static string JwtSecret { get; private set; } = "";
    public static int JwtExpiryHours { get; private set; }
    public static int MaxLoginAttemptsPerMinute { get; private set; }
    public static int AccountLockoutMinutes { get; private set; }
    public static string[] CorsOrigins { get; private set; } = [];

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("commanderApi.enabled", true);
        Port = ServerConfiguration.GetOrUpdateSetting("commanderApi.port", 8090);
        JwtSecret = ServerConfiguration.GetOrUpdateSetting(
            "commanderApi.jwtSecret",
            GenerateDefaultJwtSecret()
        );
        JwtExpiryHours = ServerConfiguration.GetOrUpdateSetting("commanderApi.jwtExpiryHours", 24);
        MaxLoginAttemptsPerMinute = ServerConfiguration.GetOrUpdateSetting(
            "commanderApi.maxLoginAttemptsPerMinute",
            10
        );
        AccountLockoutMinutes = ServerConfiguration.GetOrUpdateSetting(
            "commanderApi.accountLockoutMinutes",
            15
        );
        CorsOrigins = ServerConfiguration.GetOrUpdateSetting(
            "commanderApi.corsOrigins",
            new[] { "http://localhost:8090" }
        );

        if (Enabled)
        {
            logger.Information("Commander API configured on port {Port}", Port);
        }
    }

    private static string GenerateDefaultJwtSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
