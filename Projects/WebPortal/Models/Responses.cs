using System;

namespace Server.WebPortal.Models;

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
    public string Username { get; set; } = "";
}

public class AccountInfoResponse
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string AccessLevel { get; set; } = "";
    public bool Banned { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastLogin { get; set; }
    public int CharacterCount { get; set; }
    public int MaxCharacters { get; set; }
}

public class ServerInfoResponse
{
    public string ServerName { get; set; } = "";
    public bool Online { get; set; }
    public int PlayerCount { get; set; }
    public string ClientVersion { get; set; } = "";
    public string Expansion { get; set; } = "";
    public string ConnectionHost { get; set; } = "";
    public int ConnectionPort { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = "";
}

public class SuccessResponse
{
    public string Message { get; set; } = "";
}
