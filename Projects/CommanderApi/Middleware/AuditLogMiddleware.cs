using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Server.CommanderApi.Services;

namespace Server.CommanderApi.Middleware;

/// <summary>
///     Middleware that logs all admin API requests to the audit log.
///     Captures: actor, HTTP method, path, query string, and response status.
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;

    // Paths that are too noisy to log (e.g., status polling)
    private static readonly HashSet<string> _skipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/admin/auth/verify",
        "/api/admin/server/status"
    };

    public AuditLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuditLogService auditLog)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip noisy endpoints
        if (_skipPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        // Only audit /api/admin/ paths
        if (!path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip login endpoint (no authenticated user yet)
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

        var actor = context.User.Identity?.Name ?? "unknown";
        var action = $"{context.Request.Method} {path}";

        await _next(context);

        var success = context.Response.StatusCode is >= 200 and < 400;

        auditLog.Log(
            actor,
            action,
            path,
            context.Request.QueryString.Value,
            success
        );
    }
}
