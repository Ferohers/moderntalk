using System.ComponentModel.DataAnnotations;

namespace Server.CommanderApi.Models;

public class AdminLoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = "";
}

public class BroadcastRequest
{
    [Required(ErrorMessage = "Message is required")]
    public string Message { get; set; } = "";
}

public class ShutdownRequest
{
    /// <summary>
    ///     Whether to save the world before shutting down. Default: true
    /// </summary>
    public bool Save { get; set; } = true;
}

public class RestartRequest
{
    /// <summary>
    ///     Whether to save the world before restarting. Default: true
    /// </summary>
    public bool Save { get; set; } = true;

    /// <summary>
    ///     Countdown delay in seconds before restart. Default: 60
    /// </summary>
    [Range(0, 3600, ErrorMessage = "Delay must be between 0 and 3600 seconds")]
    public int Delay { get; set; } = 60;
}

public class KickRequest
{
    /// <summary>
    ///     Optional reason for the kick
    /// </summary>
    public string? Reason { get; set; }
}

public class BanRequest
{
    /// <summary>
    ///     Optional reason for the ban
    /// </summary>
    public string? Reason { get; set; }
}

public class ChangeAccessLevelRequest
{
    [Required(ErrorMessage = "Access level is required")]
    public string AccessLevel { get; set; } = "";
}
