using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server.Accounting;
using Server.Logging;
using Server.CommanderApi.Models;

namespace Server.CommanderApi.Services;

public class AccountService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AccountService));

    public async Task<List<AccountResponse>> SearchAccounts(string? searchTerm)
    {
        var term = searchTerm?.ToLowerInvariant() ?? "";

        return await GameThreadDispatcher.Enqueue(() =>
        {
            var accounts = new List<AccountResponse>();

            foreach (Account account in Accounts.GetAccounts())
            {
                if (string.IsNullOrEmpty(term) ||
                    account.Username.ToLowerInvariant().Contains(term))
                {
                    accounts.Add(MapToAccountResponse(account));
                }
            }

            return accounts;
        });
    }

    public async Task<AccountDetailResponse?> GetAccountDetail(string username)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var acct = Accounts.GetAccount(username) as Account;
            if (acct == null)
            {
                return null;
            }

            var characters = new List<CharacterSummaryResponse>();
            for (var i = 0; i < acct.Length; i++)
            {
                var mobile = acct[i];
                if (mobile != null)
                {
                    characters.Add(new CharacterSummaryResponse
                    {
                        Name = mobile.Name ?? "",
                        Serial = mobile.Serial.Value
                    });
                }
            }

            return new AccountDetailResponse
            {
                Username = acct.Username,
                AccessLevel = acct.AccessLevel.ToString(),
                IsBanned = acct.Banned,
                BanReason = null, // SetBanTags stores in comments; not directly queryable
                BannedBy = null,
                LastLogin = acct.LastLogin,
                Created = acct.Created,
                CharacterCount = characters.Count,
                MaxCharacters = acct.Limit,
                Email = acct.Email,
                Characters = characters
            };
        });
    }

    public async Task<(bool success, string? error)> BanAccount(string targetUsername, string actor, string? reason)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var account = Accounts.GetAccount(targetUsername) as Account;
            if (account == null)
            {
                return (false, "Account not found");
            }

            // Check access level
            var adminAccount = Accounts.GetAccount(actor) as Account;
            if (adminAccount != null && account.AccessLevel >= adminAccount.AccessLevel)
            {
                return (false, "Cannot ban an account with equal or higher access level");
            }

            account.Banned = true;
            // Note: SetBanTags(Mobile, DateTime, TimeSpan) records ban metadata but requires
            // the admin's Mobile reference which we don't have from HTTP context.
            // The Banned flag is what actually enforces the ban.

            logger.Information("Commander API: Account '{Target}' banned by {Actor}",
                targetUsername, actor);

            return (true, (string?)null);
        });
    }

    public async Task<(bool success, string? error)> UnbanAccount(string targetUsername, string actor)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var account = Accounts.GetAccount(targetUsername) as Account;
            if (account == null)
            {
                return (false, "Account not found");
            }

            account.Banned = false;

            logger.Information("Commander API: Account '{Target}' unbanned by {Actor}",
                targetUsername, actor);

            return (true, (string?)null);
        });
    }

    public async Task<(bool success, string? error)> ChangeAccessLevel(string targetUsername, string accessLevelName, string actor)
    {
        if (!Enum.TryParse<AccessLevel>(accessLevelName, true, out var newLevel))
        {
            return (false, $"Invalid access level: {accessLevelName}. Valid values: Player, Counselor, GameMaster, Seer, Administrator, Developer, Owner");
        }

        return await GameThreadDispatcher.Enqueue(() =>
        {
            var account = Accounts.GetAccount(targetUsername) as Account;
            if (account == null)
            {
                return (false, "Account not found");
            }

            // Check access level — cannot promote to or above own level
            var adminAccount = Accounts.GetAccount(actor) as Account;
            if (adminAccount != null && newLevel >= adminAccount.AccessLevel)
            {
                return (false, "Cannot set access level to equal or higher than your own");
            }

            account.AccessLevel = newLevel;

            logger.Information("Commander API: Account '{Target}' access level changed to {Level} by {Actor}",
                targetUsername, newLevel, actor);

            return (true, (string?)null);
        });
    }

    public async Task<List<AccountResponse>> GetAccountsByIp(string ip)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var accounts = new List<AccountResponse>();

            foreach (Account account in Accounts.GetAccounts())
            {
                // Check if any login IP matches
                try
                {
                    if (account.LoginIPs?.Any(loginIp =>
                        loginIp?.ToString()?.Equals(ip, StringComparison.OrdinalIgnoreCase) == true) == true)
                    {
                        accounts.Add(MapToAccountResponse(account));
                    }
                }
                catch
                {
                    // Skip accounts where IP access fails
                }
            }

            return accounts;
        });
    }

    private static AccountResponse MapToAccountResponse(Account acct)
    {
        int actualCharCount = 0;
        for (var i = 0; i < acct.Length; i++)
        {
            if (acct[i] != null)
            {
                actualCharCount++;
            }
        }

        return new AccountResponse
        {
            Username = acct.Username,
            AccessLevel = acct.AccessLevel.ToString(),
            IsBanned = acct.Banned,
            LastLogin = acct.LastLogin,
            Created = acct.Created,
            CharacterCount = actualCharCount,
            Email = acct.Email
        };
    }
}
