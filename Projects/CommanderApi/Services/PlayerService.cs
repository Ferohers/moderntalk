using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server.Accounting;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Network;
using Server.CommanderApi.Models;

namespace Server.CommanderApi.Services;

public class PlayerService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PlayerService));

    public async Task<List<PlayerResponse>> GetOnlinePlayers()
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var players = new List<PlayerResponse>();

            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile is Mobile mobile)
                {
                    players.Add(MapToPlayerResponse(mobile, ns));
                }
            }

            return players;
        });
    }

    public async Task<List<PlayerResponse>> SearchPlayers(string searchTerm)
    {
        var term = searchTerm.ToLowerInvariant();

        return await GameThreadDispatcher.Enqueue(() =>
        {
            var players = new List<PlayerResponse>();

            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile is Mobile mobile &&
                    (mobile.Name?.ToLowerInvariant().Contains(term) == true ||
                     ns.Account?.Username?.ToLowerInvariant().Contains(term) == true))
                {
                    players.Add(MapToPlayerResponse(mobile, ns));
                }
            }

            return players;
        });
    }

    public async Task<PlayerDetailResponse?> GetPlayerDetail(int serial)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile == null)
            {
                return null;
            }

            return new PlayerDetailResponse
            {
                Serial = mobile.Serial.Value,
                Name = mobile.Name ?? "",
                AccessLevel = mobile.AccessLevel.ToString(),
                X = mobile.X,
                Y = mobile.Y,
                Z = mobile.Z,
                Map = mobile.Map?.ToString() ?? "",
                Account = mobile.NetState?.Account?.Username ?? "",
                Hits = mobile.Hits,
                HitsMax = mobile.HitsMax,
                Stam = mobile.Stam,
                StamMax = mobile.StamMax,
                Mana = mobile.Mana,
                ManaMax = mobile.ManaMax,
                Str = mobile.RawStr,
                Dex = mobile.RawDex,
                Int = mobile.RawInt,
                IsHidden = mobile.Hidden,
                IsSquelched = mobile.Squelched,
                IsJailed = mobile.Region?.Name?.ToLowerInvariant().Contains("jail") ?? false,
                Region = mobile.Region?.Name ?? "",
                Body = mobile.Body,
                Hue = mobile.Hue,
                Criminal = mobile.Criminal
            };
        });
    }

    public async Task<(bool success, string? error)> KickPlayer(int serial, string actor, string? reason)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile?.NetState == null)
            {
                return (false, "Player not found or not online");
            }

            // Check access level — cannot kick equal or higher rank
            var adminAccount = Accounts.GetAccount(actor) as Account;
            if (adminAccount != null && mobile.AccessLevel >= adminAccount.AccessLevel)
            {
                return (false, "Cannot kick a player with equal or higher access level");
            }

            var kickReason = reason ?? "Kicked by administrator";
            mobile.NetState.Disconnect(kickReason);

            logger.Information("Commander API: {Player} kicked by {Actor} (reason: {Reason})",
                mobile.Name, actor, kickReason);

            return (true, (string?)null);
        });
    }

    public async Task<(bool success, string? error)> BanPlayer(int serial, string actor, string? reason)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile?.NetState == null)
            {
                return (false, "Player not found or not online");
            }

            var account = mobile.NetState.Account as Account;
            if (account == null)
            {
                return (false, "Account not found for player");
            }

            // Check access level
            var adminAccount = Accounts.GetAccount(actor) as Account;
            if (adminAccount != null && account.AccessLevel >= adminAccount.AccessLevel)
            {
                return (false, "Cannot ban an account with equal or higher access level");
            }

            account.Banned = true;
            account.SetBanTags(actor, reason ?? "Banned via Commander API");
            mobile.NetState.Disconnect("Banned by administrator");

            logger.Information("Commander API: {Player} (account: {Account}) banned by {Actor}",
                mobile.Name, account.Username, actor);

            return (true, (string?)null);
        });
    }

    public async Task<(bool success, string? error)> UnbanPlayer(int serial, string actor)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile == null)
            {
                return (false, "Player not found");
            }

            var account = mobile.NetState?.Account as Account;
            if (account == null)
            {
                return (false, "Account not found for player");
            }

            account.Banned = false;

            logger.Information("Commander API: {Player} (account: {Account}) unbanned by {Actor}",
                mobile.Name, account.Username, actor);

            return (true, (string?)null);
        });
    }

    public async Task<List<ItemResponse>?> GetEquipment(int serial)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile == null)
            {
                return null;
            }

            var items = new List<ItemResponse>();
            foreach (var item in mobile.Items)
            {
                items.Add(MapToItemResponse(item));
            }

            return items;
        });
    }

    public async Task<List<ItemResponse>?> GetBackpack(int serial)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile?.Backpack == null)
            {
                return null;
            }

            return SerializeContainer(mobile.Backpack);
        });
    }

    public async Task<List<SkillResponse>?> GetSkills(int serial)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile == null)
            {
                return null;
            }

            var skills = new List<SkillResponse>();

            try
            {
                for (int i = 0; i < mobile.Skills.Length; i++)
                {
                    var skill = mobile.Skills[i];
                    if (skill?.Base > 0)
                    {
                        skills.Add(new SkillResponse
                        {
                            Name = skill.Info.Name ?? "",
                            Value = skill.Value,
                            Base = skill.Base,
                            Cap = skill.Cap,
                            Lock = skill.Lock.ToString()
                        });
                    }
                }
            }
            catch
            {
                // Skills may not be available for all mobile types
            }

            return skills;
        });
    }

    public async Task<Dictionary<string, object>?> GetProperties(int serial)
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var mobile = World.FindMobile(serial);
            if (mobile == null)
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                ["Name"] = mobile.Name ?? "",
                ["Serial"] = mobile.Serial.Value,
                ["Body"] = mobile.Body,
                ["Hue"] = mobile.Hue,
                ["Hits"] = mobile.Hits,
                ["HitsMax"] = mobile.HitsMax,
                ["Stam"] = mobile.Stam,
                ["StamMax"] = mobile.StamMax,
                ["Mana"] = mobile.Mana,
                ["ManaMax"] = mobile.ManaMax,
                ["Str"] = mobile.RawStr,
                ["Dex"] = mobile.RawDex,
                ["Int"] = mobile.RawInt,
                ["X"] = mobile.X,
                ["Y"] = mobile.Y,
                ["Z"] = mobile.Z,
                ["Map"] = mobile.Map?.ToString() ?? "",
                ["AccessLevel"] = mobile.AccessLevel.ToString(),
                ["Hidden"] = mobile.Hidden,
                ["Blessed"] = mobile.Blessed,
                ["Criminal"] = mobile.Criminal,
                ["Region"] = mobile.Region?.Name ?? ""
            };
        });
    }

    private static PlayerResponse MapToPlayerResponse(Mobile mobile, NetState ns)
    {
        return new PlayerResponse
        {
            Serial = mobile.Serial.Value,
            Name = mobile.Name ?? "",
            AccessLevel = mobile.AccessLevel.ToString(),
            Location = $"{mobile.X},{mobile.Y},{mobile.Z}",
            Map = mobile.Map?.ToString() ?? "",
            Account = ns.Account?.Username ?? "",
            IsHidden = mobile.Hidden,
            IsSquelched = mobile.Squelched,
            IsJailed = mobile.Region?.Name?.ToLowerInvariant().Contains("jail") ?? false
        };
    }

    private static ItemResponse MapToItemResponse(Item item)
    {
        var dto = new ItemResponse
        {
            Serial = item.Serial.Value,
            Name = item.Name ?? item.GetType().Name,
            ItemId = item.ItemID,
            Hue = item.Hue,
            Amount = item.Amount,
            Layer = item.Layer.ToString(),
            Properties = GetItemProperties(item)
        };

        if (item is Container container && container.Items.Count > 0)
        {
            dto.Children = SerializeContainer(container);
        }

        return dto;
    }

    private static List<ItemResponse> SerializeContainer(Container container)
    {
        var items = new List<ItemResponse>();
        foreach (var item in container.Items)
        {
            items.Add(MapToItemResponse(item));
        }
        return items;
    }

    private static List<PropertyResponse> GetItemProperties(Item item)
    {
        var properties = new List<PropertyResponse>();
        try
        {
            var list = item.GetProperties();
            foreach (var entry in list)
            {
                properties.Add(new PropertyResponse
                {
                    Number = entry.Number,
                    Text = entry.String
                });
            }
        }
        catch
        {
            // Some items may not support GetProperties
        }
        return properties;
    }
}
