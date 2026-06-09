namespace RizaCanKilicIsTakibi.Models;

public sealed class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);
}
