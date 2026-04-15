using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Server.CommanderApi.Models;

namespace Server.CommanderApi.Services;

public class AuditLogService
{
    private readonly ConcurrentQueue<AuditLogEntry> _entries = new();
    private const int MaxEntries = 1000;

    public void Log(string actor, string action, string? target = null, string? details = null, bool success = true)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = action,
            Target = target,
            Details = details,
            Success = success
        };

        _entries.Enqueue(entry);

        // Trim old entries if we exceed the limit
        while (_entries.Count > MaxEntries)
        {
            _entries.TryDequeue(out _);
        }
    }

    public List<AuditLogEntryResponse> GetRecentEntries(int count = 100)
    {
        return _entries
            .Reverse()
            .Take(count)
            .Select(e => new AuditLogEntryResponse
            {
                Timestamp = e.Timestamp,
                Actor = e.Actor,
                Action = e.Action,
                Target = e.Target,
                Details = e.Details,
                Success = e.Success
            })
            .ToList();
    }
}

internal class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Target { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
}
