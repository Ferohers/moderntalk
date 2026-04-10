using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server.WebPortal.Models;
using Server.WebPortal.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace Server.WebPortal.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService authService, HttpResponse response) =>
        {
            // Manual validation beyond data annotations
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Username and password are required" });
            }

            if (request.Password != request.ConfirmPassword)
            {
                return Results.BadRequest(new ErrorResponse { Error = "Passwords do not match" });
            }

            var (authResponse, error) = await authService.Register(request);

            if (error != null)
            {
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            // Set tokens as HttpOnly cookies
            SetAuthCookies(response, authResponse!);

            return Results.Ok(new { authResponse!.Username, authResponse.ExpiresIn });
        });

        group.MapPost("/login", async (LoginRequest request, AuthService authService, HttpResponse response) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Username and password are required" });
            }

            var (authResponse, error) = await authService.Login(request);

            if (error != null)
            {
                return Results.Unauthorized();
            }

            // Set tokens as HttpOnly cookies
            SetAuthCookies(response, authResponse!);

            return Results.Ok(new { authResponse!.Username, authResponse.ExpiresIn });
        });

        group.MapPost("/refresh", async (RefreshRequest request, AuthService authService, TokenService tokenService, HttpResponse response) =>
        {
            var (username, error) = authService.RefreshToken(request.RefreshToken);

            if (error != null)
            {
                return Results.Unauthorized();
            }

            // Generate new tokens
            var (accessToken, refreshToken, expiresIn) = tokenService.GenerateTokens(username!);

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn,
                Username = username!
            };

            SetAuthCookies(response, authResponse);

            return Results.Ok(new { username, expiresIn });
        });

        group.MapPost("/logout", (HttpContext context, AuthService authService, TokenService tokenService) =>
        {
            var refreshToken = context.Request.Cookies["refresh_token"];
            var username = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (refreshToken != null)
            {
                authService.Logout(refreshToken, username);
            }

            // Clear cookies
            context.Response.Cookies.Delete("access_token");
            context.Response.Cookies.Delete("refresh_token");

            return Results.Ok(new SuccessResponse { Message = "Logged out successfully" });
        }).RequireAuthorization();
    }

    private static void SetAuthCookies(HttpResponse response, AuthResponse authResponse)
    {
        // Web portal runs on plain HTTP (Kestrel), so Secure must be false
        // If you add a reverse proxy with HTTPS later, set this to true
        const bool secure = false;

        // Access token - shorter expiry
        response.Cookies.Append("access_token", authResponse.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromSeconds(authResponse.ExpiresIn)
        });

        // Refresh token - longer expiry
        response.Cookies.Append("refresh_token", authResponse.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(7)
        });
    }
}
