using System.Diagnostics;
using ElaraMacro.Models;

namespace ElaraMacro.Services;

public sealed class RecorderService
{
    private readonly object _gate = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly List<RecordedEvent> _events = new();
    private Point? _lastMousePoint;

    public bool IsRecording { get; private set; }

    public void Start()
    {
        lock (_gate) { _events.Clear(); _lastMousePoint = null; _stopwatch.Restart(); IsRecording = true; }
    }

    public List<RecordedEvent> Stop()
    {
        lock (_gate) { IsRecording = false; _stopwatch.Stop(); return _events.Select(Clone).ToList(); }
    }

    public void Process(RecordedEvent e, AppSettings settings)
    {
        lock (_gate)
        {
            if (!IsRecording) return;
            if (e.Kind == EventKind.MouseMove)
            {
                var pt = new Point(e.X, e.Y);
                if (_lastMousePoint is Point prev)
                {
                    if (Math.Abs(prev.X - pt.X) < settings.MouseMoveThresholdPx &&
                        Math.Abs(prev.Y - pt.Y) < settings.MouseMoveThresholdPx) return;
                }
                _lastMousePoint = pt;
            }
            var copy = Clone(e);
            copy.TimestampMs = _stopwatch.ElapsedMilliseconds;
            _events.Add(copy);
        }
    }

    private static RecordedEvent Clone(RecordedEvent e) => new()
    {
        Kind = e.Kind, X = e.X, Y = e.Y,
        MouseData = e.MouseData, KeyCode = e.KeyCode, TimestampMs = e.TimestampMs
    };
}
