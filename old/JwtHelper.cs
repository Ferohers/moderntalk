/*************************************************************************
 * JWT Helper for ModernUO HTTP API                                       *
 * Simple JWT implementation without external dependencies               *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Server;

namespace Server.HTTP;

public static class JwtHelper
{
    public static string GenerateToken(string username, AccessLevel accessLevel, string secret, int expiryHours)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            sub = username,
            accessLevel = (int)accessLevel,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddHours(expiryHours).ToUnixTimeSeconds()
        };
        
        var headerBase64 = Base64UrlEncode(JsonSerializer.Serialize(header));
        var payloadBase64 = Base64UrlEncode(JsonSerializer.Serialize(payload));
        
        var signatureInput = $"{headerBase64}.{payloadBase64}";
        var signature = ComputeHmacSha256(signatureInput, secret);
        var signatureBase64 = Base64UrlEncode(signature);
        
        return $"{signatureInput}.{signatureBase64}";
    }
    
    public static bool ValidateToken(string token, string secret, out string username)
    {
        username = "";
        
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }
            
            var signatureInput = $"{parts[0]}.{parts[1]}";
            var expectedSignature = ComputeHmacSha256(signatureInput, secret);
            var expectedSignatureBase64 = Base64UrlEncode(expectedSignature);
            
            if (parts[2] != expectedSignatureBase64)
            {
                return false;
            }
            
            var payloadJson = Base64UrlDecode(parts[1]);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
            
            if (payload == null)
            {
                return false;
            }
            
            // Check expiration
            if (payload.TryGetValue("exp", out var expElement))
            {
                var exp = expElement.GetInt64();
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                {
                    return false;
                }
            }
            
            // Extract username
            if (payload.TryGetValue("sub", out var subElement))
            {
                username = subElement.GetString() ?? "";
                return !string.IsNullOrEmpty(username);
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Base64UrlEncode(bytes);
    }
    
    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
    
    private static string Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        var bytes = Convert.FromBase64String(padded);
        return Encoding.UTF8.GetString(bytes);
    }
    
    private static byte[] ComputeHmacSha256(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }
}
