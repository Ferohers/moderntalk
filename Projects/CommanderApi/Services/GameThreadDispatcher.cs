using System;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.CommanderApi.Services;

public static class GameThreadDispatcher
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GameThreadDispatcher));

    /// <summary>
    ///     Dispatches a function to the game thread and returns the result asynchronously.
    ///     This is the ONLY safe way to access game state from the web server threads.
    /// </summary>
    public static Task<T> Enqueue<T>(Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        Core.LoopContext.Post(() =>
        {
            try
            {
                var result = action();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Game thread dispatch failed");
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    ///     Dispatches an action to the game thread without returning a result.
    /// </summary>
    public static Task Enqueue(Action action)
    {
        return Enqueue<bool>(() =>
        {
            action();
            return true;
        });
    }
}
