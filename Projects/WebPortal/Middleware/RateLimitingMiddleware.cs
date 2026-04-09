using System.Collections.Concurrent;

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
        // Trust X-Forwarded-For only from known proxies
        var forwardedFor = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return forwardedFor;
    }

    private class RateLimitEntry
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int RequestCount { get; set; }
    }
}
