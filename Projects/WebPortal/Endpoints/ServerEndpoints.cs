using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Server;
using Server.Accounting;
using Server.Misc;
using Server.WebPortal.Configuration;
using Server.WebPortal.Models;
using Server.WebPortal.Services;

namespace Server.WebPortal.Endpoints;

public static class ServerEndpoints
{
    public static void MapServerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/server");

        group.MapGet("/info", async () =>
        {
            var playerCount = await GameThreadDispatcher.Enqueue(() =>
            {
                int count = 0;
                foreach (var acct in Accounts.GetAccounts())
                {
                    for (var i = 0; i < acct.Length; i++)
                    {
                        if (acct[i]?.NetState != null)
                        {
                            count++;
                        }
                    }
                }
                return count;
            });

            // Use auto-detected public IP, fall back to configured connection host
            var connectionHost = ServerList.PublicAddress?.ToString() ?? WebPortalConfiguration.ConnectionHost;

            return Results.Ok(new ServerInfoResponse
            {
                ServerName = WebPortalConfiguration.ServerName,
                Online = true,
                PlayerCount = playerCount,
                ClientVersion = ServerConfiguration.GetOrUpdateSetting("server.clientVersion", "7.0.96.0"),
                Expansion = Core.Expansion.ToString(),
                ConnectionHost = connectionHost,
                ConnectionPort = WebPortalConfiguration.ConnectionPort
            });
        });
    }
}
