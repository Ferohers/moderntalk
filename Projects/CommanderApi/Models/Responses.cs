using System;
using System.Collections.Generic;

namespace Server.CommanderApi.Models;

// Auth responses
public class AdminLoginResponse
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string AccessLevel { get; set; } = "";
    public int ExpiresInHours { get; set; }
}

public class TokenVerifyResponse
{
    public bool Valid { get; set; }
    public string Username { get; set; } = "";
    public string AccessLevel { get; set; } = "";
}

// Server responses
public class ServerStatusResponse
{
    public bool IsRunning { get; set; }
    public double UptimeSeconds { get; set; }
    public int PlayerCount { get; set; }
    public long MemoryUsageBytes { get; set; }
    public string Version { get; set; } = "";
    public string Expansion { get; set; } = "";
}

// Player responses
public class PlayerResponse
{
    public int Serial { get; set; }
    public string Name { get; set; } = "";
    public string AccessLevel { get; set; } = "";
    public string Location { get; set; } = "";
    public string Map { get; set; } = "";
    public string Account { get; set; } = "";
    public bool IsHidden { get; set; }
    public bool IsSquelched { get; set; }
    public bool IsJailed { get; set; }
}

public class PlayerDetailResponse
{
    public int Serial { get; set; }
    public string Name { get; set; } = "";
    public string AccessLevel { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public string Map { get; set; } = "";
    public string Account { get; set; } = "";
    public int Hits { get; set; }
    public int HitsMax { get; set; }
    public int Stam { get; set; }
    public int StamMax { get; set; }
    public int Mana { get; set; }
    public int ManaMax { get; set; }
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Int { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSquelched { get; set; }
    public bool IsJailed { get; set; }
    public string Region { get; set; } = "";
    public int Body { get; set; }
    public int Hue { get; set; }
    public bool Criminal { get; set; }
}

public class ItemResponse
{
    public int Serial { get; set; }
    public string Name { get; set; } = "";
    public int ItemId { get; set; }
    public int Hue { get; set; }
    public int Amount { get; set; }
    public string Layer { get; set; } = "";
    public List<PropertyResponse> Properties { get; set; } = new();
    public List<ItemResponse>? Children { get; set; }
}

public class PropertyResponse
{
    public int Number { get; set; }
    public string? Text { get; set; }
}

public class SkillResponse
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public double Base { get; set; }
    public int Cap { get; set; }
    public string Lock { get; set; } = "";
}

// Account responses
public class AccountResponse
{
    public string Username { get; set; } = "";
    public string AccessLevel { get; set; } = "";
    public bool IsBanned { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime Created { get; set; }
    public int CharacterCount { get; set; }
    public string? Email { get; set; }
}

public class AccountDetailResponse
{
    public string Username { get; set; } = "";
    public string AccessLevel { get; set; } = "";
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public string? BannedBy { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime Created { get; set; }
    public int CharacterCount { get; set; }
    public int MaxCharacters { get; set; }
    public string? Email { get; set; }
    public List<CharacterSummaryResponse> Characters { get; set; } = new();
}

public class CharacterSummaryResponse
{
    public string Name { get; set; } = "";
    public int Serial { get; set; }
}

// World responses
public class WorldStatsResponse
{
    public long ItemCount { get; set; }
    public long MobileCount { get; set; }
    public int PlayerCount { get; set; }
    public string Expansion { get; set; } = "";
}

// Generic responses
public class SuccessResponse
{
    public string Message { get; set; } = "";
}

public class ErrorResponse
{
    public string Error { get; set; } = "";
}

// Audit log response
public class AuditLogEntryResponse
{
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Target { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
}
