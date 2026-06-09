using ClosedXML.Excel;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Drawing;

namespace RizaCanKilicIsTakibi.Services;

public sealed class YibfExcelImportService : IYibfImportService
{
    private static readonly IReadOnlyList<(int ColumnIndex, string ColumnKey)> IsTakibiColumns =
    [
        (1, YibfIsTakibiColumnKeys.JobName),
        (2, YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi),
        (3, YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi),
        (4, YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi),
        (5, YibfIsTakibiColumnKeys.EvraklarTamMi),
        (6, YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi),
        (7, YibfIsTakibiColumnKeys.DekontAlindiMi),
        (8, YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi),
        (9, YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi),
        (10, YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi),
        (11, YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi),
        (12, YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi),
        (13, YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi)
    ];

    public Task<YibfImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(filePath);

        var anaBilgiEntries = ImportAnaBilgi(workbook.Worksheet("ANA BİLGİ"), out var anaEvents, cancellationToken);
        var isTakibiEntries = ImportIsTakibi(workbook.Worksheet("İŞ TAKİBİ"), out var cellStates, cancellationToken);

        return Task.FromResult(new YibfImportData
        {
            AnaBilgiEntries = anaBilgiEntries,
            AnaBilgiEvents = anaEvents,
            IsTakibiEntries = isTakibiEntries,
            CellStates = cellStates
        });
    }

    private static List<YibfAnaBilgiEntry> ImportAnaBilgi(IXLWorksheet worksheet, out List<YibfAnaBilgiEvent> events, CancellationToken cancellationToken)
    {
        var entries = new List<YibfAnaBilgiEntry>();
        events = new List<YibfAnaBilgiEvent>();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var displayOrder = 0;

        for (var row = 2; row <= lastRow; row += 5)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var adaParsel = worksheet.Cell(row, 1).GetString().Trim();
            var yibfNoRaw = worksheet.Cell(row, 2).GetString().Trim();
            var idare = worksheet.Cell(row + 1, 2).GetString().Trim();
            var yapiSahibi = worksheet.Cell(row + 2, 2).GetString().Trim();
            var muteahhit = worksheet.Cell(row + 3, 2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(adaParsel) && string.IsNullOrWhiteSpace(yibfNoRaw) && string.IsNullOrWhiteSpace(idare) && string.IsNullOrWhiteSpace(yapiSahibi) && string.IsNullOrWhiteSpace(muteahhit))
            {
                continue;
            }

            var entry = new YibfAnaBilgiEntry
            {
                Id = Guid.NewGuid(),
                AdaParsel = adaParsel,
                YibfNo = yibfNoRaw.Replace("YİBF NO:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(),
                Idare = idare,
                YapiSahibi = yapiSahibi,
                Muteahhit = muteahhit,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            entries.Add(entry);

            var eventOrder = 0;
            for (var column = 3; column <= lastColumn; column++)
            {
                var dateCell = worksheet.Cell(row, column);
                var descCell = worksheet.Cell(row + 1, column);

                var description = descCell.GetString().Trim();
                var noteText = descCell.HasComment
                    ? descCell.GetComment().Text?.Trim() ?? string.Empty
                    : dateCell.HasComment ? dateCell.GetComment().Text?.Trim() ?? string.Empty : string.Empty;
                var backgroundColor = ResolveColor(descCell);
                if (string.IsNullOrWhiteSpace(backgroundColor))
                {
                    backgroundColor = ResolveColor(dateCell);
                }

                var eventDate = TryReadDate(dateCell);
                if (string.IsNullOrWhiteSpace(description) && eventDate is null && string.IsNullOrWhiteSpace(noteText) && string.IsNullOrWhiteSpace(backgroundColor))
                {
                    continue;
                }

                events.Add(new YibfAnaBilgiEvent
                {
                    Id = Guid.NewGuid(),
                    EntryId = entry.Id,
                    EventDate = eventDate,
                    Description = description,
                    BackgroundColor = backgroundColor,
                    NoteText = noteText,
                    DisplayOrder = eventOrder++
                });
            }
        }

        return entries;
    }

    private static List<YibfIsTakibiEntry> ImportIsTakibi(IXLWorksheet worksheet, out List<YibfCellState> cellStates, CancellationToken cancellationToken)
    {
        var entries = new List<YibfIsTakibiEntry>();
        cellStates = new List<YibfCellState>();

        var displayOrder = 0;
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (row.Cells(1, 13).All(cell => string.IsNullOrWhiteSpace(cell.GetString())))
            {
                continue;
            }

            var entry = new YibfIsTakibiEntry
            {
                Id = Guid.NewGuid(),
                JobName = row.Cell(1).GetString().Trim(),
                MuellifBilgileriGeldiMi = row.Cell(2).GetString().Trim(),
                DenetciAtamalariYapildiMi = row.Cell(3).GetString().Trim(),
                TumProjelerinDijitaliVarMi = row.Cell(4).GetString().Trim(),
                EvraklarTamMi = row.Cell(5).GetString().Trim(),
                YibfSozlesmeHazirlandiMi = row.Cell(6).GetString().Trim(),
                DekontAlindiMi = row.Cell(7).GetString().Trim(),
                RuhsatBasvurusuYapildiMi = row.Cell(8).GetString().Trim(),
                RuhsatNushasiAlindiMi = row.Cell(9).GetString().Trim(),
                IsyeriTeslimTutangiHazirlandiMi = row.Cell(10).GetString().Trim(),
                IsgYazisiHazirlandiMi = row.Cell(11).GetString().Trim(),
                SaglikGuvenlikPlaniGeldiMi = row.Cell(12).GetString().Trim(),
                TemelTopraklamaTutanagiHazirlandiMi = row.Cell(13).GetString().Trim(),
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(entry.JobName))
            {
                continue;
            }

            entries.Add(entry);

            foreach (var (columnIndex, columnKey) in IsTakibiColumns)
            {
                var cell = row.Cell(columnIndex);
                var noteText = cell.HasComment ? cell.GetComment().Text?.Trim() ?? string.Empty : string.Empty;
                var backgroundColor = ResolveColor(cell);
                if (string.IsNullOrWhiteSpace(noteText) && string.IsNullOrWhiteSpace(backgroundColor))
                {
                    continue;
                }

                cellStates.Add(new YibfCellState
                {
                    EntryId = entry.Id,
                    ColumnKey = columnKey,
                    BackgroundColor = backgroundColor,
                    NoteText = noteText
                });
            }
        }

        return entries;
    }

    private static DateTime? TryReadDate(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var date))
        {
            return date;
        }

        return DateTime.TryParse(cell.GetString().Trim(), out var parsed) ? parsed : null;
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
