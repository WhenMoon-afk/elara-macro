using ElaraMacro.Models;

namespace ElaraMacro.Services;

public sealed class RecorderService : IDisposable
{
    private readonly HookManager _hookManager;
    private readonly Func<AppSettings> _settingsAccessor;
    private readonly object _gate = new();
    private readonly List<RecordedEvent> _events = new();
    private bool _isRecording;
    private Point? _lastMousePoint;

    public RecorderService(HookManager hookManager, Func<AppSettings> settingsAccessor)
    {
        _hookManager = hookManager;
        _settingsAccessor = settingsAccessor;

        _hookManager.KeyDown += OnHookEvent;
        _hookManager.KeyUp += OnHookEvent;
        _hookManager.MouseDown += OnHookEvent;
        _hookManager.MouseUp += OnHookEvent;
        _hookManager.MouseMove += OnHookEvent;
        _hookManager.MouseWheel += OnHookEvent;
    }

    public void Start()
    {
        lock (_gate)
        {
            _events.Clear();
            _lastMousePoint = null;
            _isRecording = true;
        }
    }

    public List<RecordedEvent> Stop()
    {
        lock (_gate)
        {
            _isRecording = false;
            return _events.Select(Clone).ToList();
        }
    }

    private void OnHookEvent(object? _, RecordedEvent e)
    {
        lock (_gate)
        {
            if (!_isRecording)
            {
                return;
            }

            if (e.Kind == EventKind.MouseMove)
            {
                var current = new Point(e.X, e.Y);
                if (_lastMousePoint is Point previous)
                {
                    var deltaX = current.X - previous.X;
                    var deltaY = current.Y - previous.Y;
                    var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                    if (distance < Math.Max(0, _settingsAccessor().MouseMoveThresholdPx))
                    {
                        return;
                    }
                }

                _lastMousePoint = current;
            }

            var recorded = Clone(e);
            recorded.TimestampMs = Environment.TickCount64;
            _events.Add(recorded);
        }
    }

    private static RecordedEvent Clone(RecordedEvent e) => new()
    {
        Kind = e.Kind,
        X = e.X,
        Y = e.Y,
        MouseData = e.MouseData,
        KeyCode = e.KeyCode,
        TimestampMs = e.TimestampMs
    };

    public void Dispose()
    {
        _hookManager.KeyDown -= OnHookEvent;
        _hookManager.KeyUp -= OnHookEvent;
        _hookManager.MouseDown -= OnHookEvent;
        _hookManager.MouseUp -= OnHookEvent;
        _hookManager.MouseMove -= OnHookEvent;
        _hookManager.MouseWheel -= OnHookEvent;
    }
}
