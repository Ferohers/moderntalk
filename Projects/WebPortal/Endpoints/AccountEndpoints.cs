using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server.WebPortal.Models;
using Server.WebPortal.Services;
using System.IdentityModel.Tokens.Jwt;

namespace Server.WebPortal.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/account");

        group.MapGet("/info", async (HttpContext context, AccountService accountService) =>
        {
            var username = context.User?.Identity?.Name ??
                          context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            var (response, error) = await accountService.GetAccountInfo(username);

            if (error != null)
            {
                return Results.NotFound(new ErrorResponse { Error = error });
            }

            return Results.Ok(response);
        });

        group.MapPost("/change-password", async (HttpContext context, ChangePasswordRequest request, AccountService accountService) =>
        {
            var username = context.User?.Identity?.Name ??
                          context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return Results.BadRequest(new ErrorResponse { Error = "New passwords do not match" });
            }

            var (success, error) = await accountService.ChangePassword(username, request);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(new SuccessResponse { Message = "Password changed successfully" });
        });
    }
}
