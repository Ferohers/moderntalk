using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server.CommanderApi.Models;
using Server.CommanderApi.Services;

namespace Server.CommanderApi.Endpoints;

public static class ServerEndpoints
{
    public static void MapServerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/server")
            .RequireAuthorization();

        group.MapGet("/status", async (ServerService serverService) =>
        {
            var status = await serverService.GetStatus();
            return Results.Ok(status);
        });

        group.MapPost("/save", async (HttpContext context, ServerService serverService, AuditLogService auditLog) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            await serverService.SaveWorld(actor);
            auditLog.Log(actor, "ServerSave", null, null, true);
            return Results.Ok(new SuccessResponse { Message = "World save completed" });
        });

        group.MapPost("/shutdown", async (HttpContext context, ServerService serverService, AuditLogService auditLog, ShutdownRequest? request) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var save = request?.Save ?? true;
            await serverService.Shutdown(save, actor);
            auditLog.Log(actor, "ServerShutdown", null, $"save={save}", true);
            return Results.Ok(new SuccessResponse { Message = "Server shutting down" });
        });

        group.MapPost("/restart", async (HttpContext context, ServerService serverService, AuditLogService auditLog, RestartRequest? request) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var save = request?.Save ?? true;
            var delay = request?.Delay ?? 60;
            await serverService.Restart(save, delay, actor);
            auditLog.Log(actor, "ServerRestart", null, $"save={save}, delay={delay}s", true);
            return Results.Ok(new SuccessResponse { Message = $"Restart scheduled in {delay} seconds" });
        });

        group.MapPost("/broadcast", async (HttpContext context, ServerService serverService, AuditLogService auditLog, BroadcastRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Message is required" });
            }

            var actor = context.User.Identity?.Name ?? "unknown";
            await serverService.Broadcast(request.Message, actor);
            auditLog.Log(actor, "Broadcast", null, request.Message, true);
            return Results.Ok(new SuccessResponse { Message = "Message broadcasted" });
        });

        group.MapPost("/staff-message", async (HttpContext context, ServerService serverService, AuditLogService auditLog, BroadcastRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Message is required" });
            }

            var actor = context.User.Identity?.Name ?? "unknown";
            await serverService.StaffMessage(request.Message, actor);
            auditLog.Log(actor, "StaffMessage", null, request.Message, true);
            return Results.Ok(new SuccessResponse { Message = "Staff message sent" });
        });
    }
}
