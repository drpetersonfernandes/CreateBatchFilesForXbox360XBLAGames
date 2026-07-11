using Serilog.Core;
using Serilog.Events;

namespace CreateBatchFilesForXbox360XBLAGames;

public class UILogSink : ILogEventSink
{
    private static Action<string>? _writeAction;

    public static void Initialize(Action<string> writeAction)
    {
        _writeAction = writeAction;
    }

    public void Emit(LogEvent logEvent)
    {
        var action = _writeAction;
        if (action == null) return;

        var message = logEvent.RenderMessage();
        action(message);
    }
}
