namespace RizaCanKilicIsTakibi.Models;

public sealed class ExcelWorkbookExportModel
{
    public IReadOnlyList<ExcelSheetExportModel> Sheets { get; init; } = Array.Empty<ExcelSheetExportModel>();
}

public sealed class ExcelSheetExportModel
{
    public string Name { get; init; } = "Sheet1";
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ExcelRowExportModel> Rows { get; init; } = Array.Empty<ExcelRowExportModel>();
}

public sealed class ExcelRowExportModel
{
    public IReadOnlyList<ExcelCellExportModel> Cells { get; init; } = Array.Empty<ExcelCellExportModel>();
}

public sealed class ExcelCellExportModel
{
    public string Value { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string BackgroundColor { get; init; } = string.Empty;
}
