namespace RizaCanKilicIsTakibi.Models;

public sealed class ReportPackExportModel
{
    public string Title { get; init; } = "RIZA CAN KILIÇ İŞ TAKİBİ RAPOR PAKETİ";
    public IList<ReportPackSectionModel> Sections { get; init; } = [];
}

public sealed class ReportPackSectionModel
{
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> Headers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];
}
