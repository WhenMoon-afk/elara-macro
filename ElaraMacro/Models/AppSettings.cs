namespace ElaraMacro.Models;

public sealed class AppSettings
{
    public Keys RecordHotkey { get; set; } = Keys.F9;
    public Keys PlayHotkey { get; set; } = Keys.F10;
    public Keys PauseHotkey { get; set; } = Keys.F11;
    public Keys StopHotkey { get; set; } = Keys.F12;
    public int LoopCount { get; set; } = 1;
    public bool NormalizeTiming { get; set; }
    public int NormalizedDelayMs { get; set; } = 50;
    public int MouseMoveThresholdPx { get; set; } = 5;
    public bool AlwaysOnTop { get; set; } = true;
    public int WindowX { get; set; } = 120;
    public int WindowY { get; set; } = 120;
}
