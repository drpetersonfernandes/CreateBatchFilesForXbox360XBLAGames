using System.Diagnostics;

namespace CreateBatchFilesForXbox360XBLAGames;

internal static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        task.ContinueWith(static t =>
        {
            var ex = t.Exception?.Flatten();
            Debug.WriteLine($"[FireAndForget] Unobserved task exception: {ex}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
