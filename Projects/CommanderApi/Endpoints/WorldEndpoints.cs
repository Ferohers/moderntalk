using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server;
using Server.CommanderApi.Models;
using Server.CommanderApi.Services;
using Server.Items;
using Server.Logging;
using Server.Network;

namespace Server.CommanderApi.Endpoints;

public static class WorldEndpoints
{
    public static void MapWorldEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/world")
            .RequireAuthorization();

        group.MapGet("/stats", async () =>
        {
            var stats = await GameThreadDispatcher.Enqueue(() =>
            {
                int playerCount = 0;
                foreach (var ns in NetState.Instances)
                {
                    if (ns.Mobile != null)
                    {
                        playerCount++;
                    }
                }

                return new WorldStatsResponse
                {
                    ItemCount = World.Items.Count,
                    MobileCount = World.Mobiles.Count,
                    PlayerCount = playerCount,
                    Expansion = Core.Expansion.ToString()
                };
            });

            return Results.Ok(stats);
        });

        group.MapGet("/items/{serial:uint}", async (uint serial) =>
        {
            var item = await GameThreadDispatcher.Enqueue(() =>
            {
                var target = World.FindItem(new Serial(serial));
                if (target == null)
                {
                    return null;
                }

                return new Dictionary<string, object>
                {
                    ["Serial"] = target.Serial.Value,
                    ["Name"] = target.Name ?? target.GetType().Name,
                    ["ItemId"] = target.ItemID,
                    ["Hue"] = target.Hue,
                    ["Amount"] = target.Amount,
                    ["Layer"] = target.Layer.ToString(),
                    ["Location"] = $"{target.X},{target.Y},{target.Z}",
                    ["Map"] = target.Map?.ToString() ?? "",
                    ["Movable"] = target.Movable,
                    ["Visible"] = target.Visible
                };
            });

            if (item == null)
            {
                return Results.NotFound(new ErrorResponse { Error = "Item not found" });
            }

            return Results.Ok(item);
        });

        group.MapGet("/audit-log", (AuditLogService auditLog, int? count) =>
        {
            var entries = auditLog.GetRecentEntries(count ?? 100);
            return Results.Ok(entries);
        });
    }
}
