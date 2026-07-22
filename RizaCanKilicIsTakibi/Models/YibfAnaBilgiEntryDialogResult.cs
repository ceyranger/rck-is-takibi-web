namespace RizaCanKilicIsTakibi.Models;

public sealed class YibfAnaBilgiEntryDialogResult
{
    public Guid? ProjectId { get; init; }
    public Guid? WorkGroupId { get; init; }
    public string AdaParsel { get; init; } = string.Empty;
    public string YibfNo { get; init; } = string.Empty;
    public string Idare { get; init; } = string.Empty;
    public string YapiSahibi { get; init; } = string.Empty;
    public string Muteahhit { get; init; } = string.Empty;
}