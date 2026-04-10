using System;
using System.Threading.Tasks;
using Server.Accounting;
using Server.Logging;
using Server.Misc;
using Server.WebPortal.Middleware;
using Server.WebPortal.Models;

namespace Server.WebPortal.Services;

public class AuthService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AuthService));
    private readonly TokenService _tokenService;
    private readonly AccountLockoutService _lockoutService;

    public AuthService(TokenService tokenService, AccountLockoutService lockoutService)
    {
        _tokenService = tokenService;
        _lockoutService = lockoutService;
    }

    public async Task<(AuthResponse? response, string? error)> Register(RegisterRequest request)
    {
        // Validate username using the same rules as the game
        if (!AccountHandler.IsValidUsername(request.Username))
        {
            return (null, "Invalid username. Must be 3-30 characters and cannot contain < > : \" / \\ | ? *");
        }

        // Validate password
        if (!AccountHandler.IsValidPassword(request.Password))
        {
            return (null, "Invalid password format");
        }

        // Check if account already exists - dispatch to game thread
        var existingAccount = await GameThreadDispatcher.Enqueue(() => Accounts.GetAccount(request.Username));

        if (existingAccount != null)
        {
            // Anti-enumeration: return generic error
            return (null, "Bu hesabı açamadık. Başka bir isim deneyiniz.");
        }

        // Create the account on the game thread
        var account = await GameThreadDispatcher.Enqueue(() =>
        {
            try
            {
                return new Account(request.Username, request.Password);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "{Username} isimli hesabı açarken sorun oldu", request.Username);
                return null;
            }
        });

        if (account == null)
        {
            return (null, "Unable to create account. Please try again.");
        }

        // Set email if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await GameThreadDispatcher.Enqueue(() => account.Email = request.Email);
        }

        // Generate tokens
        var (accessToken, refreshToken, expiresIn) = _tokenService.GenerateTokens(request.Username);

        logger.Information("Web Portal: Account '{Username}' registered", request.Username);

        return (new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            Username = request.Username
        }, null);
    }

    public async Task<(AuthResponse? response, string? error)> Login(LoginRequest request)
    {
        // Check account lockout
        var lockoutResult = _lockoutService.CheckLockout(request.Username);
        if (lockoutResult.IsLocked)
        {
            return (null, "Account is temporarily locked due to too many failed attempts. Please try again later.");
        }

        // Validate credentials on the game thread
        var validationResult = await GameThreadDispatcher.Enqueue(() =>
        {
            var acct = Accounts.GetAccount(request.Username) as Account;
            if (acct == null)
            {
                return (false, false); // not found, not valid
            }

            if (acct.Banned)
            {
                return (false, true); // not found (anti-enumeration), banned
            }

            var valid = acct.CheckPassword(request.Password);
            return (valid, false);
        });

        var (passwordValid, isBanned) = validationResult;

        if (!passwordValid)
        {
            _lockoutService.RecordFailedAttempt(request.Username);

            // Anti-enumeration: same error message regardless of whether username exists
            return (null, "Hatalı giriş yaptınız");
        }

        if (isBanned)
        {
            return (null, "Bu hesap banlanmış");
        }

        // Success - clear lockout and generate tokens
        _lockoutService.RecordSuccessfulLogin(request.Username);

        var (accessToken, refreshToken, expiresIn) = _tokenService.GenerateTokens(request.Username);

        logger.Information("Web Portal: Account '{Username}' logged in", request.Username);

        return (new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            Username = request.Username
        }, null);
    }

    public (string? username, string? error) RefreshToken(string refreshToken)
    {
        var username = _tokenService.ValidateRefreshToken(refreshToken);
        if (username == null)
        {
            return (null, "Invalid or expired refresh token");
        }

        // Invalidate the old refresh token (rotation)
        _tokenService.InvalidateRefreshToken(refreshToken);

        return (username, null);
    }

    public void Logout(string refreshToken, string? username)
    {
        _tokenService.InvalidateRefreshToken(refreshToken);

        if (username != null)
        {
            logger.Information("Web Portal: Account '{Username}' logged out", username);
        }
    }
}
