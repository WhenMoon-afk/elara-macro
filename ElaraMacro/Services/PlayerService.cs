using ElaraMacro.Models;

namespace ElaraMacro.Services;

public sealed class PlayerService
{
    private readonly InputSimulatorService _simulator;
    private readonly SemaphoreSlim _pauseSemaphore = new(1, 1);
    private volatile bool _isPaused;

    public PlayerService(InputSimulatorService simulator)
    {
        _simulator = simulator;
    }

    public void Pause()
    {
        if (_isPaused)
        {
            return;
        }

        _pauseSemaphore.Wait();
        _isPaused = true;
    }

    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        _pauseSemaphore.Release();
    }

    public async Task PlayAsync(List<RecordedEvent> events, AppSettings settings, CancellationToken ct)
    {
        if (events.Count == 0)
        {
            return;
        }

        var orderedEvents = events.OrderBy(e => e.TimestampMs).ToList();
        var loops = settings.LoopCount == 0 ? int.MaxValue : Math.Max(0, settings.LoopCount);

        for (var loopIndex = 0; loopIndex < loops; loopIndex++)
        {
            for (var index = 0; index < orderedEvents.Count; index++)
            {
                ct.ThrowIfCancellationRequested();
                await _pauseSemaphore.WaitAsync(ct).ConfigureAwait(false);
                _pauseSemaphore.Release();

                var delayMs = ComputeDelay(index, orderedEvents, settings);
                if (delayMs > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(delayMs, ct).ConfigureAwait(false);
                }

                _simulator.Replay(orderedEvents[index]);
            }
        }
    }

    private static int ComputeDelay(int index, IReadOnlyList<RecordedEvent> orderedEvents, AppSettings settings)
    {
        if (index == 0)
        {
            return 0;
        }

        if (settings.NormalizeTiming)
        {
            return Math.Max(0, settings.NormalizedDelayMs);
        }

        var delta = orderedEvents[index].TimestampMs - orderedEvents[index - 1].TimestampMs;
        return (int)Math.Max(0, delta);
    }
}
