namespace ElaraMacro.Models;

public sealed class RecordedEvent
{
    public EventKind Kind { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int MouseData { get; set; }
    public Keys KeyCode { get; set; }
    public long TimestampMs { get; set; }
}
