using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server.CommanderApi.Models;
using Server.CommanderApi.Services;

namespace Server.CommanderApi.Endpoints;

public static class PlayerEndpoints
{
    public static void MapPlayerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/players")
            .RequireAuthorization();

        group.MapGet("/", async (PlayerService playerService) =>
        {
            var players = await playerService.GetOnlinePlayers();
            return Results.Ok(players);
        });

        group.MapGet("/search", async (string? name, PlayerService playerService) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Search name parameter is required" });
            }

            var players = await playerService.SearchPlayers(name);
            return Results.Ok(players);
        });

        group.MapGet("/{serial:uint}", async (uint serial, PlayerService playerService) =>
        {
            var player = await playerService.GetPlayerDetail(serial);
            if (player == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Player not found" });
            }

            return Results.Ok(player);
        });

        group.MapPost("/{serial:uint}/kick", async (uint serial, HttpContext context, PlayerService playerService, AuditLogService auditLog, KickRequest? request) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var (success, error) = await playerService.KickPlayer(serial, actor, request?.Reason);
            auditLog.Log(actor, "KickPlayer", $"serial:{serial}", request?.Reason, success);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(new SuccessResponse { Message = "Player kicked" });
        });

        group.MapPost("/{serial:uint}/ban", async (uint serial, HttpContext context, PlayerService playerService, AuditLogService auditLog, BanRequest? request) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var (success, error) = await playerService.BanPlayer(serial, actor, request?.Reason);
            auditLog.Log(actor, "BanPlayer", $"serial:{serial}", request?.Reason, success);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(new SuccessResponse { Message = "Player banned" });
        });

        group.MapPost("/{serial:uint}/unban", async (uint serial, HttpContext context, PlayerService playerService, AuditLogService auditLog) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var (success, error) = await playerService.UnbanPlayer(serial, actor);
            auditLog.Log(actor, "UnbanPlayer", $"serial:{serial}", null, success);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(new SuccessResponse { Message = "Player unbanned" });
        });

        group.MapGet("/{serial:uint}/equipment", async (uint serial, PlayerService playerService) =>
        {
            var items = await playerService.GetEquipment(serial);
            if (items == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Player not found" });
            }

            return Results.Ok(items);
        });

        group.MapGet("/{serial:uint}/backpack", async (uint serial, PlayerService playerService) =>
        {
            var items = await playerService.GetBackpack(serial);
            if (items == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Player or backpack not found" });
            }

            return Results.Ok(items);
        });

        group.MapGet("/{serial:uint}/skills", async (uint serial, PlayerService playerService) =>
        {
            var skills = await playerService.GetSkills(serial);
            if (skills == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Player not found" });
            }

            return Results.Ok(skills);
        });

        group.MapGet("/{serial:uint}/properties", async (uint serial, PlayerService playerService) =>
        {
            var properties = await playerService.GetProperties(serial);
            if (properties == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Player not found" });
            }

            return Results.Ok(properties);
        });
    }
}
