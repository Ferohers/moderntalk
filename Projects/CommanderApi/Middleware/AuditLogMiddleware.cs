using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Server.CommanderApi.Services;

namespace Server.CommanderApi.Middleware;

/// <summary>
///     Middleware that selectively logs admin API mutations to the audit log.
///     Read-only requests (GET/HEAD/OPTIONS) are never audited — they produce
///     no meaningful audit trail and only pollute the log.
///     All mutation endpoints (POST/PUT/DELETE) — including login — log their
///     own descriptive audit entries explicitly in their endpoint handlers,
///     so no middleware-level logging is needed.
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

        // All mutations (POST/PUT/DELETE) — including login — are logged
        // explicitly by their endpoint handlers with descriptive action names,
        // targets, and details. No middleware-level logging needed.
        await _next(context);
    }
}
