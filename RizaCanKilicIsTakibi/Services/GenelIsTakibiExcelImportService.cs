using ClosedXML.Excel;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class GenelIsTakibiExcelImportService : IGenelIsTakibiExcelImportService
{
    public GenelIsTakibiExcelImportResult ImportFromFile(string filePath, string aksiyonaEkleneceklerDistrict = "GENEL")
    {
        using var workbook = new XLWorkbook(filePath);

        var genelSheet = FindSheet(workbook, "GENEL İŞLER");
        var aksiyonSheet = FindSheet(workbook, "AKSİYON");
        var aksiyonaEkleneceklerSheet = FindSheet(workbook, "AKSİYONA EKLENECEKLER");
        var eksikProjeSheet = FindSheet(workbook, "EKSİK PROJE");

        var tasks = ReadGenelTasks(genelSheet);
        var actionEntries = ReadActionEntries(aksiyonSheet)
            .Concat(ReadActionToAddEntries(aksiyonaEkleneceklerSheet, aksiyonaEkleneceklerDistrict))
            .ToList();
        var missingProjectEntries = ReadMissingProjectEntries(eksikProjeSheet);

        return new GenelIsTakibiExcelImportResult
        {
            Tasks = tasks,
            ActionEntries = actionEntries,
            MissingProjectEntries = missingProjectEntries
        };
    }

    private static IXLWorksheet FindSheet(XLWorkbook workbook, string sheetName)
    {
        var target = NormalizeText(sheetName);
        var match = workbook.Worksheets.FirstOrDefault(sheet => NormalizeText(sheet.Name) == target);
        if (match is not null)
        {
            return match;
        }

        var available = string.Join(", ", workbook.Worksheets.Select(sheet => sheet.Name));
        throw new InvalidOperationException($"'{sheetName}' sayfası bulunamadı. Mevcut sayfalar: {available}");
    }

    private static List<TaskItem> ReadGenelTasks(IXLWorksheet worksheet)
    {
        var rows = new List<TaskItem>();
        TaskBoardType? currentBoard = null;
        var sortOrders = new Dictionary<TaskBoardType, int>
        {
            [TaskBoardType.Acil] = 0,
            [TaskBoardType.Genel] = 0
        };

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var rowIndex = 1; rowIndex <= lastRow; rowIndex++)
        {
            var rawTitle = TextOrEmpty(worksheet.Cell(rowIndex, 1).GetString());
            var rawDescription = TextOrEmpty(worksheet.Cell(rowIndex, 2).GetString());
            var normalizedTitle = NormalizeText(rawTitle);

            if (normalizedTitle == "acil yapilacak is")
            {
                currentBoard = TaskBoardType.Acil;
                continue;
            }

            if (normalizedTitle == "genel yapilacak is")
            {
                currentBoard = TaskBoardType.Genel;
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawTitle) || currentBoard is null)
            {
                continue;
            }

            var board = currentBoard.Value;
            rows.Add(new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = rawTitle,
                Description = rawDescription,
                BoardType = board,
                SortOrder = sortOrders[board]++,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        return rows;
    }

    private static List<ActionEntry> ReadActionEntries(IXLWorksheet worksheet)
    {
        var rows = new List<ActionEntry>();
        var currentDistrict = string.Empty;
        var displayOrders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var rowIndex = 2; rowIndex <= lastRow; rowIndex++)
        {
            var district = TextOrEmpty(worksheet.Cell(rowIndex, 1).GetString());
            var ownerParcel = TextOrEmpty(worksheet.Cell(rowIndex, 2).GetString());
            var workText = TextOrEmpty(worksheet.Cell(rowIndex, 3).GetString());

            if (!string.IsNullOrWhiteSpace(district))
            {
                currentDistrict = district;
            }

            if (string.IsNullOrWhiteSpace(ownerParcel) && string.IsNullOrWhiteSpace(workText))
            {
                continue;
            }

            if (!displayOrders.TryGetValue(currentDistrict, out var order))
            {
                order = 0;
            }

            displayOrders[currentDistrict] = order + 1;
            rows.Add(new ActionEntry
            {
                Id = Guid.NewGuid(),
                Category = ActionEntryCategory.Aksiyon,
                District = currentDistrict,
                OwnerParcelText = ownerParcel,
                WorkText = workText,
                DisplayOrder = order,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        return rows;
    }

    private static List<ActionEntry> ReadActionToAddEntries(IXLWorksheet worksheet, string district)
    {
        var rows = new List<ActionEntry>();
        var displayOrder = 0;
        var targetDistrict = string.IsNullOrWhiteSpace(district) ? "GENEL" : district.Trim();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var rowIndex = 2; rowIndex <= lastRow; rowIndex++)
        {
            var ownerParcel = TextOrEmpty(worksheet.Cell(rowIndex, 1).GetString());
            var workText = TextOrEmpty(worksheet.Cell(rowIndex, 2).GetString());
            if (string.IsNullOrWhiteSpace(ownerParcel) && string.IsNullOrWhiteSpace(workText))
            {
                continue;
            }

            rows.Add(new ActionEntry
            {
                Id = Guid.NewGuid(),
                Category = ActionEntryCategory.AksiyonaEklenecekler,
                District = targetDistrict,
                OwnerParcelText = ownerParcel,
                WorkText = workText,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        return rows;
    }

    private static List<MissingProjectEntry> ReadMissingProjectEntries(IXLWorksheet worksheet)
    {
        var rows = new List<MissingProjectEntry>();
        var displayOrder = 0;
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var rowIndex = 2; rowIndex <= lastRow; rowIndex++)
        {
            var adaParsel = TextOrEmpty(worksheet.Cell(rowIndex, 1).GetString());
            var yapiSahibi = TextOrEmpty(worksheet.Cell(rowIndex, 2).GetString());
            var mediumTextRaw = TextOrEmpty(worksheet.Cell(rowIndex, 3).GetString());
            var missingProjectText = TextOrEmpty(worksheet.Cell(rowIndex, 4).GetString());
            var description = TextOrEmpty(worksheet.Cell(rowIndex, 5).GetString());

            if (string.IsNullOrWhiteSpace(adaParsel)
                && string.IsNullOrWhiteSpace(yapiSahibi)
                && string.IsNullOrWhiteSpace(mediumTextRaw)
                && string.IsNullOrWhiteSpace(missingProjectText)
                && string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            var medium = ParseMissingMedium(mediumTextRaw);
            rows.Add(new MissingProjectEntry
            {
                Id = Guid.NewGuid(),
                AdaParsel = adaParsel,
                YapiSahibi = yapiSahibi,
                RecordMedium = medium,
                RecordMediumText = MissingProjectMediumLabelProvider.GetLabel(medium),
                MissingProjectText = missingProjectText,
                Description = description,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        return rows;
    }

    private static MissingProjectMedium ParseMissingMedium(string value)
    {
        var normalized = NormalizeText(value);
        var hasDijital = normalized.Contains("dijital", StringComparison.Ordinal);
        var hasFiziki = normalized.Contains("fiziki", StringComparison.Ordinal)
            || normalized.Contains("fiziksel", StringComparison.Ordinal)
            || normalized.Contains("fizik", StringComparison.Ordinal);

        if (hasDijital && hasFiziki)
        {
            return MissingProjectMedium.FizikiVeDijital;
        }

        if (hasDijital)
        {
            return MissingProjectMedium.Dijital;
        }

        return MissingProjectMedium.Fiziki;
    }

    private static string TextOrEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim().ToLower(CultureInfo.GetCultureInfo("tr-TR"))
            .Replace('ı', 'i')
            .Replace('İ', 'i');
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }
}
