using ClosedXML.Excel;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Drawing;

namespace RizaCanKilicIsTakibi.Services;

public sealed class TadilatExcelImportService : ITadilatImportService
{
    private static readonly IReadOnlyList<(int ColumnIndex, string ColumnKey)> StateColumns =
    [
        (2, TadilatColumnKeys.JobName),
        (3, TadilatColumnKeys.ProjectType),
        (4, TadilatColumnKeys.DigitalReceived),
        (5, TadilatColumnKeys.InspectorApproved),
        (6, TadilatColumnKeys.OutputAndReportArrived),
        (7, TadilatColumnKeys.OfficialLetterSubmitted),
        (8, TadilatColumnKeys.ArchivedFromMunicipality),
        (9, TadilatColumnKeys.Description1),
        (10, TadilatColumnKeys.Description2)
    ];

    public Task<TadilatImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(filePath);

        var entries = new List<TadilatEntry>();
        var cellStates = new List<TadilatCellState>();
        var displayOrderByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var worksheet in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subTab = worksheet.Name.Equals("AKTİF", StringComparison.OrdinalIgnoreCase)
                ? TadilatSubTab.Aktif
                : TadilatSubTab.Biten;

            var usedRange = worksheet.RangeUsed();
            if (usedRange is null)
            {
                continue;
            }

            var firstDataRow = usedRange.RangeAddress.FirstAddress.RowNumber + 1;
            var lastDataRow = usedRange.RangeAddress.LastAddress.RowNumber;
            if (firstDataRow > lastDataRow)
            {
                continue;
            }

            var lastDistrict = string.Empty;
            for (var rowNumber = firstDataRow; rowNumber <= lastDataRow; rowNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = worksheet.Row(rowNumber);

                var rowValues = Enumerable.Range(1, 10).Select(index => row.Cell(index).GetString().Trim()).ToList();
                if (rowValues.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var district = row.Cell(1).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(district))
                {
                    lastDistrict = district;
                }
                else
                {
                    district = lastDistrict;
                }

                var jobName = row.Cell(2).GetString().Trim();
                if (string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(jobName))
                {
                    continue;
                }

                var orderKey = $"{(int)subTab}:{district}";
                displayOrderByKey.TryGetValue(orderKey, out var currentOrder);

                var entry = new TadilatEntry
                {
                    Id = Guid.NewGuid(),
                    SubTab = subTab,
                    District = district,
                    JobName = jobName,
                    ProjectType = row.Cell(3).GetString().Trim(),
                    DigitalReceived = row.Cell(4).GetString().Trim(),
                    InspectorApproved = row.Cell(5).GetString().Trim(),
                    OutputAndReportArrived = row.Cell(6).GetString().Trim(),
                    OfficialLetterSubmitted = row.Cell(7).GetString().Trim(),
                    ArchivedFromMunicipality = row.Cell(8).GetString().Trim(),
                    Description1 = row.Cell(9).GetString().Trim(),
                    Description2 = row.Cell(10).GetString().Trim(),
                    DisplayOrder = currentOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                displayOrderByKey[orderKey] = currentOrder + 1;
                entries.Add(entry);

                foreach (var (columnIndex, columnKey) in StateColumns)
                {
                    var cell = row.Cell(columnIndex);
                    var noteText = cell.HasComment ? cell.GetComment().Text?.Trim() ?? string.Empty : string.Empty;
                    var backgroundColor = ResolveColor(cell);

                    if (string.IsNullOrWhiteSpace(noteText) && string.IsNullOrWhiteSpace(backgroundColor))
                    {
                        continue;
                    }

                    cellStates.Add(new TadilatCellState
                    {
                        EntryId = entry.Id,
                        ColumnKey = columnKey,
                        BackgroundColor = backgroundColor,
                        NoteText = noteText
                    });
                }
            }
        }

        return Task.FromResult(new TadilatImportData
        {
            Entries = entries,
            CellStates = cellStates
        });
    }

    private static string ResolveColor(IXLCell cell)
    {
        if (cell.Style.Fill.PatternType == XLFillPatternValues.None)
        {
            return string.Empty;
        }

        var candidates = new[]
        {
            cell.Style.Fill.PatternColor,
            cell.Style.Fill.BackgroundColor
        };

        foreach (var color in candidates)
        {
            var resolved = TryResolveColor(color);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return string.Empty;
    }

    private static string TryResolveColor(XLColor color)
    {
        if (color is null || !color.HasValue)
        {
            return string.Empty;
        }

        try
        {
            return color.ColorType switch
            {
                XLColorType.Color => NormalizeColor(color.Color),
                XLColorType.Theme => NormalizeColor(color.Color),
                XLColorType.Indexed => XLColor.IndexedColors.TryGetValue(color.Indexed, out var indexedColor)
                    ? NormalizeColor(indexedColor.Color)
                    : string.Empty,
                _ => string.Empty
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeColor(Color drawingColor)
    {
        if (drawingColor == Color.Empty || drawingColor == Color.Transparent)
        {
            return string.Empty;
        }

        if (drawingColor.A == 0 || drawingColor.ToArgb() == Color.White.ToArgb())
        {
            return string.Empty;
        }

        return $"#{drawingColor.A:X2}{drawingColor.R:X2}{drawingColor.G:X2}{drawingColor.B:X2}";
    }
}
