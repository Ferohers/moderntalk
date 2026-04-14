using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server.CommanderApi.Models;
using Server.CommanderApi.Services;

namespace Server.CommanderApi.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/accounts")
            .RequireAuthorization();

        group.MapGet("/", async (string? search, AccountService accountService) =>
        {
            var accounts = await accountService.SearchAccounts(search);
            return Results.Ok(accounts);
        });

        group.MapGet("/search", async (string? username, AccountService accountService) =>
        {
            var accounts = await accountService.SearchAccounts(username);
            return Results.Ok(accounts);
        });

        group.MapGet("/by-ip/{ip}", async (string ip, AccountService accountService) =>
        {
            var accounts = await accountService.GetAccountsByIp(ip);
            return Results.Ok(accounts);
        });

        group.MapGet("/{username}", async (string username, AccountService accountService) =>
        {
            var account = await accountService.GetAccountDetail(username);
            if (account == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Account not found" });
            }

            return Results.Ok(account);
        });

        group.MapPost("/{username}/ban", async (string username, HttpContext context, AccountService accountService, AuditLogService auditLog, BanRequest? request) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var (success, error) = await accountService.BanAccount(username, actor, request?.Reason);
            auditLog.Log(actor, "BanAccount", username, request?.Reason, success);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(new SuccessResponse { Message = "Account banned" });
        });

        group.MapPost("/{username}/unban", async (string username, HttpContext context, AccountService accountService, AuditLogService auditLog) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var (success, error) = await accountService.UnbanAccount(username, actor);
            auditLog.Log(actor, "UnbanAccount", username, null, success);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(new SuccessResponse { Message = "Account unbanned" });
        });

        group.MapPost("/{username}/access-level", async (string username, HttpContext context, AccountService accountService, AuditLogService auditLog, ChangeAccessLevelRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.AccessLevel))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Access level is required" });
            }

            var actor = context.User.Identity?.Name ?? "unknown";
            var (success, error) = await accountService.ChangeAccessLevel(username, request.AccessLevel, actor);
            auditLog.Log(actor, "ChangeAccessLevel", username, $"new level: {request.AccessLevel}", success);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(new SuccessResponse { Message = $"Access level changed to {request.AccessLevel}" });
        });

        group.MapGet("/{username}/characters", async (string username, AccountService accountService) =>
        {
            var account = await accountService.GetAccountDetail(username);
            if (account == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Account not found" });
            }

            return Results.Ok(account.Characters);
        });
    }
}
