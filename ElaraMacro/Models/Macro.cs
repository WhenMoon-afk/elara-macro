namespace ElaraMacro.Models;

public sealed class Macro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Macro";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<RecordedEvent> Events { get; set; } = new();

    public override string ToString() => Name;
}
