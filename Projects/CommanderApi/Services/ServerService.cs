using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Server;
using Server.Accounting;
using Server.Logging;
using Server.Misc;
using Server.Network;
using Server.CommanderApi.Models;

namespace Server.CommanderApi.Services;

public class ServerService
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ServerService));

    public async Task<ServerStatusResponse> GetStatus()
    {
        return await GameThreadDispatcher.Enqueue(() =>
        {
            var process = Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();

            int playerCount = 0;
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null)
                {
                    playerCount++;
                }
            }

            return new ServerStatusResponse
            {
                IsRunning = true,
                UptimeSeconds = uptime.TotalSeconds,
                PlayerCount = playerCount,
                MemoryUsageBytes = GC.GetGCMemoryInfo().HeapSizeBytes,
                Version = typeof(Core).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                Expansion = Core.Expansion.ToString()
            };
        });
    }

    public async Task SaveWorld(string actor)
    {
        await GameThreadDispatcher.Enqueue(() =>
        {
            World.Save();
        });

        logger.Information("Commander API: World saved by {Actor}", actor);
    }

    public async Task Shutdown(bool save, string actor)
    {
        logger.Information("Commander API: Shutdown initiated by {Actor} (save={Save})", actor, save);

        await GameThreadDispatcher.Enqueue(() =>
        {
            if (save)
            {
                World.Save();
            }

            NetState.Shutdown();
            Core.Kill(false);
        });
    }

    public async Task Restart(bool save, int delaySeconds, string actor)
    {
        logger.Information("Commander API: Restart initiated by {Actor} (save={Save}, delay={Delay}s)", actor, save, delaySeconds);

        await GameThreadDispatcher.Enqueue(() =>
        {
            if (delaySeconds > 0)
            {
                World.Broadcast(0x35, true, $"Server will restart in {delaySeconds} seconds. Please find a safe location.");
            }

            var restartAction = () =>
            {
                if (save)
                {
                    World.Save();
                }

                World.Broadcast(0x26, true, "Server restarting now...");
                NetState.Shutdown();
                Core.Kill(true);
            };

            if (delaySeconds > 0)
            {
                var restartTimer = new ActionTimer(TimeSpan.FromSeconds(delaySeconds), restartAction);
                restartTimer.Start();
            }
            else
            {
                restartAction();
            }
        });
    }

    public async Task Broadcast(string message, string actor)
    {
        await GameThreadDispatcher.Enqueue(() =>
        {
            World.Broadcast(0x35, true, message);
        });

        logger.Information("Commander API: Broadcast by {Actor}: {Message}", actor, message);
    }

    public async Task StaffMessage(string message, string actor)
    {
        await GameThreadDispatcher.Enqueue(() =>
        {
            World.BroadcastStaff(message);
        });

        logger.Information("Commander API: Staff message by {Actor}: {Message}", actor, message);
    }

    /// <summary>
    ///     Simple timer that executes an Action callback once when it ticks.
    /// </summary>
    private class ActionTimer : Timer
    {
        private readonly Action _callback;

        public ActionTimer(TimeSpan delay, Action callback) : base(delay, 1)
        {
            _callback = callback;
        }

        protected override void OnTick()
        {
            _callback?.Invoke();
        }
    }
}
