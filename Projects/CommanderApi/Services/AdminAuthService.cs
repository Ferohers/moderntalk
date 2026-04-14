using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Server.Accounting;
using Server.CommanderApi.Configuration;
using Server.CommanderApi.Models;
using Server.Logging;

namespace Server.CommanderApi.Services;

public class AdminAuthService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AdminAuthService));
    private readonly ConcurrentDictionary<string, LoginAttemptInfo> _loginAttempts = new();

    public async Task<(AdminLoginResponse? response, string? error)> Login(AdminLoginRequest request)
    {
        // Check rate limiting
        var attemptKey = request.Username.ToLowerInvariant();
        if (_loginAttempts.TryGetValue(attemptKey, out var attempts) && attempts.IsLockedOut)
        {
            logger.Warning("Commander API: Login attempt locked out for '{Username}'", request.Username);
            return (null, "Too many failed attempts. Please try again later.");
        }

        // Validate credentials on the game thread
        var validationResult = await GameThreadDispatcher.Enqueue(() =>
        {
            var acct = Accounts.GetAccount(request.Username) as Account;
            if (acct == null)
            {
                return (false, false, (AccessLevel)(-1)); // not found, not valid, no level
            }

            if (acct.Banned)
            {
                return (false, true, acct.AccessLevel); // not found (anti-enumeration), banned
            }

            var valid = acct.CheckPassword(request.Password);
            return (valid, false, acct.AccessLevel);
        });

        var (passwordValid, isBanned, accessLevel) = validationResult;

        if (!passwordValid)
        {
            RecordFailedAttempt(attemptKey);
            return (null, "Invalid credentials");
        }

        if (isBanned)
        {
            return (null, "Account is banned");
        }

        // Check GameMaster+ access
        if (accessLevel < AccessLevel.GameMaster)
        {
            return (null, "Insufficient privileges. GameMaster access or higher is required.");
        }

        // Success — clear failed attempts
        _loginAttempts.TryRemove(attemptKey, out _);

        // Generate JWT token
        var token = GenerateToken(request.Username, accessLevel);

        logger.Information("Commander API: Admin '{Username}' ({AccessLevel}) logged in", request.Username, accessLevel);

        return (new AdminLoginResponse
        {
            Token = token,
            Username = request.Username,
            AccessLevel = accessLevel.ToString(),
            ExpiresInHours = CommanderApiConfiguration.JwtExpiryHours
        }, null);
    }

    public (string username, string accessLevel)? ValidateToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(CommanderApiConfiguration.JwtSecret);
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "ModernUO-CommanderApi",
                ValidateAudience = true,
                ValidAudience = "ModernUO-CommanderApi-Client",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                RequireSignedTokens = true
            };

            var principal = tokenHandler.ValidateToken(token, parameters, out _);
            var username = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var accessLevel = principal.FindFirst("AccessLevel")?.Value;

            if (username == null || accessLevel == null)
            {
                return null;
            }

            return (username, accessLevel);
        }
        catch
        {
            return null;
        }
    }

    private string GenerateToken(string username, AccessLevel accessLevel)
    {
        var key = Encoding.UTF8.GetBytes(CommanderApiConfiguration.JwtSecret);
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim("AccessLevel", accessLevel.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(CommanderApiConfiguration.JwtExpiryHours),
            Issuer = "ModernUO-CommanderApi",
            Audience = "ModernUO-CommanderApi-Client",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    private void RecordFailedAttempt(string key)
    {
        _loginAttempts.AddOrUpdate(
            key,
            _ => new LoginAttemptInfo { Attempts = 1, FirstAttempt = DateTime.UtcNow },
            (_, existing) =>
            {
                // Reset window if enough time has passed
                if (DateTime.UtcNow - existing.FirstAttempt > TimeSpan.FromMinutes(1))
                {
                    return new LoginAttemptInfo { Attempts = 1, FirstAttempt = DateTime.UtcNow };
                }

                existing.Attempts++;
                return existing;
            }
        );
    }

    private class LoginAttemptInfo
    {
        public int Attempts { get; set; }
        public DateTime FirstAttempt { get; set; }

        public bool IsLockedOut =>
            Attempts >= CommanderApiConfiguration.MaxLoginAttemptsPerMinute &&
            DateTime.UtcNow - FirstAttempt < TimeSpan.FromMinutes(CommanderApiConfiguration.AccountLockoutMinutes);
    }
}
