using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Server.WebPortal.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;

    // Per-IP rate limiters for different endpoint categories
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _authLimits = new();
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _generalLimits = new();
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _registerLimits = new();

    private static readonly TimeSpan _authWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _generalWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _registerWindow = TimeSpan.FromHours(1);

    private const int AuthMaxRequests = 5;
    private const int GeneralMaxRequests = 60;
    private const int RegisterMaxRequests = 3;

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = GetClientIp(context);
        var path = context.Request.Path.Value ?? "";

        // Check rate limits based on endpoint category
        if (path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase))
        {
            if (IsRateLimited(_registerLimits, ip, RegisterMaxRequests, _registerWindow, out var retryAfter))
            {
                context.Response.StatusCode = 429;
                context.Response.Headers.RetryAfter = retryAfter.ToString();
                await context.Response.WriteAsJsonAsync(new { error = "Too many registration attempts. Please try again later." });
                return;
            }
        }
        else if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
        {
            if (IsRateLimited(_authLimits, ip, AuthMaxRequests, _authWindow, out var retryAfter))
            {
                context.Response.StatusCode = 429;
                context.Response.Headers.RetryAfter = retryAfter.ToString();
                await context.Response.WriteAsJsonAsync(new { error = "Too many login attempts. Please try again later." });
                return;
            }
        }

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            if (IsRateLimited(_generalLimits, ip, GeneralMaxRequests, _generalWindow, out var retryAfter))
            {
                context.Response.StatusCode = 429;
                context.Response.Headers.RetryAfter = retryAfter.ToString();
                await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please slow down." });
                return;
            }
        }

        await _next(context);
    }

    private static bool IsRateLimited(
        ConcurrentDictionary<string, RateLimitEntry> limits,
        string ip,
        int maxRequests,
        TimeSpan window,
        out int retryAfterSeconds
    )
    {
        var now = DateTime.UtcNow;
        var entry = limits.GetOrAdd(ip, _ => new RateLimitEntry());

        lock (entry)
        {
            if (now - entry.WindowStart > window)
            {
                entry.WindowStart = now;
                entry.RequestCount = 0;
            }

            entry.RequestCount++;
            retryAfterSeconds = (int)(window - (now - entry.WindowStart)).TotalSeconds;

            return entry.RequestCount > maxRequests;
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        // When ForwardedHeaders middleware is active (BehindReverseProxy=true),
        // RemoteIpAddress already contains the real client IP. Check it first.
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (remoteIp != null && !IsPrivateOrLoopbackIp(remoteIp))
        {
            return remoteIp;
        }

        // Fallback: check X-Forwarded-For header manually
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // X-Forwarded-For: client, proxy1, proxy2 — first entry is the real client
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(firstIp))
            {
                return firstIp;
            }
        }

        // Fallback: check X-Real-IP header (used by some proxies)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.Trim();
        }

        return remoteIp ?? "unknown";
    }

    private static bool IsPrivateOrLoopbackIp(string ip)
    {
        if (ip == "127.0.0.1" || ip == "::1")
            return true;

        if (ip.StartsWith("10.") || ip.StartsWith("192.168."))
            return true;

        // 172.16.0.0/12 — 172.16.x.x through 172.31.x.x
        if (ip.StartsWith("172."))
        {
            var parts = ip.Split('.');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var secondOctet) && secondOctet >= 16 && secondOctet <= 31)
                return true;
        }

        // IPv6 unique local addresses (fc00::/7)
        if (ip.StartsWith("fc") || ip.StartsWith("fd"))
            return true;

        return false;
    }

    private class RateLimitEntry
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int RequestCount { get; set; }
    }
}
