using ElaraMacro.Models;

namespace ElaraMacro.Services;

public sealed class PlayerService
{
    private readonly InputSimulatorService _simulator = new();
    private CancellationTokenSource? _cts;
    private readonly ManualResetEventSlim _pauseEvent = new(true);

    public bool IsRunning { get; private set; }
    public bool IsPaused { get; private set; }

    public event Action<AppState, string>? StatusChanged;
    public event Action? PlaybackFinished;

    public void Start(Macro macro, AppSettings settings)
    {
        if (IsRunning || macro.Events.Count == 0) return;
        IsRunning = true; IsPaused = false;
        _pauseEvent.Set();
        _cts = new CancellationTokenSource();
        var loops = settings.LoopCount <= 0 ? int.MaxValue : settings.LoopCount;

        Task.Run(async () =>
        {
            try
            {
                for (var loop = 1; loop <= loops; loop++)
                {
                    StatusChanged?.Invoke(AppState.Playing, settings.LoopCount <= 0
                        ? $"Playing - loop {loop} of \u221e"
                        : $"Playing - loop {loop} of {settings.LoopCount}");

                    long previous = 0;
                    foreach (var e in macro.Events)
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        _pauseEvent.Wait(_cts.Token);
                        var delay = settings.NormalizeTiming
                            ? settings.NormalizedDelayMs
                            : (int)Math.Max(0, e.TimestampMs - previous);
                        previous = e.TimestampMs;
                        if (delay > 0) await Task.Delay(delay, _cts.Token);
                        _simulator.ReplayEvent(e);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsRunning = false; IsPaused = false;
                _pauseEvent.Set();
                StatusChanged?.Invoke(AppState.Idle, "Idle");
                PlaybackFinished?.Invoke();
            }
        }, _cts.Token);
    }

    public void TogglePause()
    {
        if (!IsRunning) return;
        if (IsPaused) { IsPaused = false; _pauseEvent.Set(); StatusChanged?.Invoke(AppState.Playing, "Playing"); }
        else { IsPaused = true; _pauseEvent.Reset(); StatusChanged?.Invoke(AppState.Paused, "Paused"); }
    }

    public void Stop() => _cts?.Cancel();
}
