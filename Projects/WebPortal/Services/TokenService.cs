using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Server.WebPortal.Configuration;

namespace Server.WebPortal.Services;

public class TokenService
{
    private static readonly ConcurrentDictionary<string, RefreshTokenEntry> _refreshTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly SigningCredentials _signingCredentials;

    public TokenService()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(WebPortalConfiguration.JwtSecret));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public (string accessToken, string refreshToken, int expiresIn) GenerateTokens(string username)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(WebPortalConfiguration.AccessTokenExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = _signingCredentials,
            Issuer = "ModernUO",
            Audience = "ModernUO-WebPortal"
        };

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.WriteToken(handler.CreateToken(tokenDescriptor));

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = now.AddDays(WebPortalConfiguration.RefreshTokenExpiryDays);

        // Store refresh token
        _refreshTokens[refreshToken] = new RefreshTokenEntry
        {
            Username = username,
            ExpiresAt = refreshTokenExpiry
        };

        var expiresIn = (int)(expiresAt - now).TotalSeconds;

        return (accessToken, refreshToken, expiresIn);
    }

    public string? ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingCredentials.Key,
                ValidateIssuer = true,
                ValidIssuer = "ModernUO",
                ValidateAudience = true,
                ValidAudience = "ModernUO-WebPortal",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public string? ValidateRefreshToken(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var entry))
        {
            return null;
        }

        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            _refreshTokens.TryRemove(refreshToken, out _);
            return null;
        }

        return entry.Username;
    }

    public void InvalidateRefreshToken(string refreshToken)
    {
        _refreshTokens.TryRemove(refreshToken, out _);
    }

    public void InvalidateAllRefreshTokens(string username)
    {
        foreach (var kvp in _refreshTokens)
        {
            if (kvp.Value.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                _refreshTokens.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private class RefreshTokenEntry
    {
        public string Username { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}
