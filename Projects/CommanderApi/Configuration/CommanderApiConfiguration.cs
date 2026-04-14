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
    public static string CorsOrigins { get; private set; } = "";

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
        // Store CORS origins as comma-separated string since GetOrUpdateSetting<T> requires value types
        // Default is empty — native apps (Swift, etc.) don't enforce CORS, and the fallback is AllowAnyOrigin()
        CorsOrigins = ServerConfiguration.GetOrUpdateSetting(
            "commanderApi.corsOrigins",
            ""
        );

        if (Enabled)
        {
            logger.Information("Commander API configured on port {Port}", Port);
        }
    }

    /// <summary>
    ///     Returns CORS origins parsed from the comma-separated configuration string.
    /// </summary>
    public static string[] GetCorsOriginArray()
    {
        if (string.IsNullOrWhiteSpace(CorsOrigins))
        {
            return [];
        }

        return CorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string GenerateDefaultJwtSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
