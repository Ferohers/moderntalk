using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server.WebPortal.Models;
using Server.WebPortal.Services;
using Server.Accounting;
using Server.Misc;
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
                // Return 400 (not 401) so the frontend shows the actual error message
                // instead of triggering a token refresh flow
                return Results.BadRequest(new ErrorResponse { Error = error });
            }

            // Set tokens as HttpOnly cookies
            SetAuthCookies(response, authResponse!);

            return Results.Ok(new { authResponse!.Username, authResponse.ExpiresIn });
        });

        group.MapPost("/refresh", async (HttpContext context, AuthService authService, TokenService tokenService, HttpResponse response) =>
        {
            // Read refresh token from HttpOnly cookie (not request body)
            var refreshToken = context.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Results.Unauthorized();
            }

            var (username, error) = authService.RefreshToken(refreshToken);

            if (error != null)
            {
                return Results.Unauthorized();
            }

            // Generate new tokens
            var (accessToken, newRefreshToken, expiresIn) = tokenService.GenerateTokens(username!);

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = expiresIn,
                Username = username!
            };

            SetAuthCookies(response, authResponse);

            return Results.Ok(new { username, expiresIn });
        });

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, TokenService tokenService, EmailService emailService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Username and email are required" });
            }

            // Look up the account and verify email matches
            var account = await GameThreadDispatcher.Enqueue(() =>
            {
                var acct = Accounts.GetAccount(request.Username) as Account;
                if (acct == null || acct.Banned)
                {
                    return (Account?)null;
                }

                // Verify email matches (case-insensitive)
                if (string.IsNullOrWhiteSpace(acct.Email) ||
                    !acct.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return (Account?)null;
                }

                return acct;
            });

            if (account == null)
            {
                // Anti-enumeration: always return success even if account not found
                return Results.Ok(new { message = "If an account with that username and email exists, a reset link has been sent." });
            }

            // Generate reset token
            var resetToken = tokenService.GeneratePasswordResetToken(request.Username);
            if (resetToken == null)
            {
                return Results.Ok(new { message = "If an account with that username and email exists, a reset link has been sent." });
            }

            // Send email
            var emailSent = await emailService.SendPasswordResetEmail(request.Email, request.Username, resetToken);

            if (!emailSent)
            {
                // SMTP not configured — return the token directly for testing
                // In production with SMTP configured, this branch should not be hit
                return Results.Ok(new
                {
                    message = "Password reset requested. SMTP is not configured, so the reset token is returned directly.",
                    resetToken = resetToken
                });
            }

            return Results.Ok(new { message = "If an account with that username and email exists, a reset link has been sent." });
        });

        group.MapPost("/reset-password", async (ResetPasswordRequest request, TokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.ResetToken) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Reset token and new password are required" });
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return Results.BadRequest(new ErrorResponse { Error = "Passwords do not match" });
            }

            // Validate the reset token
            var username = tokenService.ValidatePasswordResetToken(request.ResetToken);
            if (username == null)
            {
                return Results.BadRequest(new ErrorResponse { Error = "Invalid or expired reset token" });
            }

            // Validate new password format
            if (!AccountHandler.IsValidPassword(request.NewPassword))
            {
                return Results.BadRequest(new ErrorResponse { Error = "Invalid new password format" });
            }

            // Reset the password on the game thread
            var result = await GameThreadDispatcher.Enqueue(() =>
            {
                var acct = Accounts.GetAccount(username) as Account;
                if (acct == null)
                {
                    return (false, "Account not found");
                }

                acct.SetPassword(request.NewPassword);
                return (true, (string?)null);
            });

            if (!result.Item1)
            {
                return Results.BadRequest(new ErrorResponse { Error = result.Item2 });
            }

            // Invalidate all refresh tokens for this user (force re-login)
            tokenService.InvalidateAllRefreshTokens(username);

            return Results.Ok(new SuccessResponse { Message = "Password has been reset successfully. Please log in with your new password." });
        });

        group.MapPost("/logout", (HttpContext context, AuthService authService, TokenService tokenService) =>
        {
            var refreshToken = context.Request.Cookies["refresh_token"];
            var username = context.User.Identity?.Name;

            if (refreshToken != null)
            {
                authService.Logout(refreshToken, username);
            }

            // Clear cookies - must match the paths used when setting them
            context.Response.Cookies.Delete("access_token", new CookieOptions { Path = "/api/" });
            context.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth/" });

            return Results.Ok(new SuccessResponse { Message = "Logged out successfully" });
        }).RequireAuthorization();
    }

    private static void SetAuthCookies(HttpResponse response, AuthResponse authResponse)
    {
        // Web portal runs on plain HTTP (Kestrel), so Secure must be false
        // If you add a reverse proxy with HTTPS later, set this to true
        const bool secure = false;

        // Access token - shorter expiry, scoped to /api/ path only
        response.Cookies.Append("access_token", authResponse.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/api/",
            MaxAge = TimeSpan.FromSeconds(authResponse.ExpiresIn)
        });

        // Refresh token - longer expiry, scoped to /api/auth/ (used by refresh and logout)
        response.Cookies.Append("refresh_token", authResponse.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth/",
            MaxAge = TimeSpan.FromDays(7)
        });
    }
}
