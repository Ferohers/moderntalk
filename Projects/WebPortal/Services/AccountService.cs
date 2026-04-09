using Server.Accounting;
using Server.Logging;
using Server.Misc;
using Server.WebPortal.Models;

namespace Server.WebPortal.Services;

public class AccountService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AccountService));

    public async Task<(AccountInfoResponse? response, string? error)> GetAccountInfo(string username)
    {
        var info = await GameThreadDispatcher.Enqueue(() =>
        {
            var acct = Accounts.GetAccount(username) as Account;
            if (acct == null)
            {
                return null;
            }

            int charCount = 0;
            for (var i = 0; i < acct.Length; i++)
            {
                if (acct[i] != null)
                {
                    charCount++;
                }
            }

            return new AccountInfoResponse
            {
                Username = acct.Username,
                Email = acct.Email ?? "",
                AccessLevel = acct.AccessLevel.ToString(),
                Banned = acct.Banned,
                Created = acct.Created,
                LastLogin = acct.LastLogin,
                CharacterCount = charCount,
                MaxCharacters = acct.Limit
            };
        });

        if (info == null)
        {
            return (null, "Account not found");
        }

        return (info, null);
    }

    public async Task<(bool success, string? error)> ChangePassword(string username, ChangePasswordRequest request)
    {
        // Validate new password
        if (!AccountHandler.IsValidPassword(request.NewPassword))
        {
            return (false, "Invalid new password format");
        }

        // Verify current password and change on game thread
        var result = await GameThreadDispatcher.Enqueue(() =>
        {
            var acct = Accounts.GetAccount(username) as Account;
            if (acct == null)
            {
                return (false, "Account not found");
            }

            // Verify current password
            if (!acct.CheckPassword(request.CurrentPassword))
            {
                return (false, "Current password is incorrect");
            }

            // Verify new password is different
            if (acct.CheckPassword(request.NewPassword))
            {
                return (false, "New password must be different from current password");
            }

            // Set the new password - uses Argon2 via Account.SetPassword
            acct.SetPassword(request.NewPassword);
            return (true, (string?)null);
        });

        if (result.Item1)
        {
            logger.Information("Web Portal: Password changed for account '{Username}'", username);
        }

        return result;
    }
}
