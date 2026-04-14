using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Server.CommanderApi.Middleware;

/// <summary>
///     Simple per-IP rate limiting middleware for the Commander API.
///     Limits the number of requests per IP address within a time window.
/// </summary>
public class AdminRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();
    private const int MaxRequestsPerMinute = 60;

    public AdminRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Only rate-limit admin API paths
        if (!path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{clientIp}:{path}";

        var entry = _entries.AddOrUpdate(
            key,
            _ => new RateLimitEntry { Count = 1, WindowStart = DateTime.UtcNow },
            (_, existing) =>
            {
                // Reset window if enough time has passed
                if (DateTime.UtcNow - existing.WindowStart > TimeSpan.FromMinutes(1))
                {
                    return new RateLimitEntry { Count = 1, WindowStart = DateTime.UtcNow };
                }

                existing.Count++;
                return existing;
            }
        );

        if (entry.Count > MaxRequestsPerMinute)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Please try again later." });
            return;
        }

        await _next(context);
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
