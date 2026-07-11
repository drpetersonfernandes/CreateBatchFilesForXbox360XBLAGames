using Serilog;

namespace CreateBatchFilesForXbox360XBLAGames;

internal static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        task.ContinueWith(static t =>
        {
            var ex = t.Exception?.Flatten();
            if (ex != null)
                Log.Warning(ex, "Unobserved task exception");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
