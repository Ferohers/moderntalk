using Server.WebPortal.Configuration;

namespace Server.WebPortal.Middleware;

public class AccountLockoutService
{
    private readonly Dictionary<string, LockoutEntry> _lockoutEntries = new(StringComparer.OrdinalIgnoreCase);

    public LockoutResult CheckLockout(string username)
    {
        if (!_lockoutEntries.TryGetValue(username, out var entry))
        {
            return LockoutResult.Allowed;
        }

        if (entry.LockedUntil.HasValue && entry.LockedUntil.Value > DateTime.UtcNow)
        {
            return new LockoutResult
            {
                IsLocked = true,
                LockedUntil = entry.LockedUntil.Value,
                FailedAttempts = entry.FailedAttempts
            };
        }

        // Lockout expired, reset
        if (entry.LockedUntil.HasValue && entry.LockedUntil.Value <= DateTime.UtcNow)
        {
            entry.FailedAttempts = 0;
            entry.LockedUntil = null;
            return LockoutResult.Allowed;
        }

        return LockoutResult.Allowed;
    }

    public void RecordFailedAttempt(string username)
    {
        if (!_lockoutEntries.TryGetValue(username, out var entry))
        {
            entry = new LockoutEntry();
            _lockoutEntries[username] = entry;
        }

        entry.FailedAttempts++;
        entry.LastAttemptAt = DateTime.UtcNow;

        // Progressive backoff
        var lockoutMinutes = entry.FailedAttempts switch
        {
            >= 15 => 1440,    // 24 hours
            >= 10 => 60,      // 1 hour
            >= 5  => WebPortalConfiguration.AccountLockoutMinutes,
            _     => 0
        };

        if (lockoutMinutes > 0)
        {
            entry.LockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
    }

    public void RecordSuccessfulLogin(string username)
    {
        _lockoutEntries.Remove(username);
    }

    private class LockoutEntry
    {
        public int FailedAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime LastAttemptAt { get; set; }
    }
}

public class LockoutResult
{
    public static LockoutResult Allowed { get; } = new() { IsLocked = false };

    public bool IsLocked { get; init; }
    public DateTime LockedUntil { get; init; }
    public int FailedAttempts { get; init; }
}
