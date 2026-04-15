using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Server.CommanderApi.Services;

namespace Server.CommanderApi.Middleware;

/// <summary>
///     Middleware that selectively logs admin API mutations to the audit log.
///     Read-only requests (GET/HEAD/OPTIONS) are never audited — they produce
///     no meaningful audit trail and only pollute the log.
///     Mutation endpoints (POST/PUT/DELETE) log their own descriptive audit
///     entries explicitly, so the middleware only handles the special login case.
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuditLogService auditLog)
    {
        var path = context.Request.Path.Value ?? "";

        // Only audit /api/admin/ paths
        if (!path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip read-only requests — they have no audit value
        var method = context.Request.Method;
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Special case: login endpoint has no authenticated user yet
        if (path.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);

            // Log successful logins after the fact
            if (context.Response.StatusCode == 200)
            {
                auditLog.Log(
                    context.Request.Headers.TryGetValue("X-Username", out var username) ? username.ToString() : "unknown",
                    "Login",
                    null,
                    null,
                    true
                );
            }

            return;
        }

        // All other mutations (POST/PUT/DELETE) are logged explicitly by
        // their endpoint handlers with descriptive action names and targets.
        // No middleware-level logging needed.
        await _next(context);
    }
}
