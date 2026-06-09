namespace RizaCanKilicIsTakibi.Models;

public sealed class TaskNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
