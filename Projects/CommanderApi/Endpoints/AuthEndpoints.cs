using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server.CommanderApi.Models;
using Server.CommanderApi.Services;

namespace Server.CommanderApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/auth");

        // Login is anonymous — anyone can attempt to log in
        group.MapPost("/login", async (AdminLoginRequest request, AdminAuthService authService, HttpResponse response) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Username and password are required" });
            }

            var (loginResponse, error) = await authService.Login(request);

            if (error != null)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            return Results.Ok(loginResponse);
        }).AllowAnonymous();

        // Verify and logout require a valid JWT
        group.MapGet("/verify", (HttpContext context, AdminAuthService authService) =>
        {
            var token = ExtractBearerToken(context);
            if (token == null)
            {
                return Results.Unauthorized();
            }

            var result = authService.ValidateToken(token);
            if (result == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new TokenVerifyResponse
            {
                Valid = true,
                Username = result.Value.username,
                AccessLevel = result.Value.accessLevel
            });
        }).RequireAuthorization();

        group.MapPost("/logout", (HttpContext context) =>
        {
            // JWT is stateless — logout is handled client-side by discarding the token.
            // This endpoint exists for API completeness and audit logging.
            return Results.Ok(new SuccessResponse { Message = "Logged out successfully" });
        }).RequireAuthorization();
    }

    private static string? ExtractBearerToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }
}
