using ClosedXML.Excel;
using QuestPDF.Infrastructure;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class ImportExportVisualPolishTests
{
    static ImportExportVisualPolishTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task ExportWorkbookAsync_Applies_Header_Filter_And_Zebra()
    {
        var path = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", $"{Guid.NewGuid():N}.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        try
        {
            var service = new ImportExportService();
            await service.ExportWorkbookAsync(new ExcelWorkbookExportModel
            {
                Sheets =
                [
                    new ExcelSheetExportModel
                    {
                        Name = "Test",
                        Headers = ["A", "B"],
                        Rows =
                        [
                            new ExcelRowExportModel
                            {
                                Cells =
                                [
                                    new ExcelCellExportModel { Value = "r1c1" },
                                    new ExcelCellExportModel { Value = "r1c2", BackgroundColor = "#FFFF0000" }
                                ]
                            },
                            new ExcelRowExportModel
                            {
                                Cells =
                                [
                                    new ExcelCellExportModel { Value = "r2c1" },
                                    new ExcelCellExportModel { Value = "r2c2" }
                                ]
                            }
                        ]
                    }
                ]
            }, path);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet(1);
            Assert.True(sheet.AutoFilter.IsEnabled);
            Assert.Equal(XLColor.FromHtml("#1F3147").Color, sheet.Cell(1, 1).Style.Fill.BackgroundColor.Color);
            Assert.Equal(XLColor.FromHtml("#FF0000").Color, sheet.Cell(2, 2).Style.Fill.BackgroundColor.Color);
            Assert.Equal(XLColor.FromHtml("#F5F8FB").Color, sheet.Cell(3, 1).Style.Fill.BackgroundColor.Color);
            Assert.Equal(XLBorderStyleValues.Thin, sheet.Cell(2, 1).Style.Border.LeftBorder);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ExportReportPackAsync_Writes_Pdf_With_Cell_Colors()
    {
        var path = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", $"{Guid.NewGuid():N}.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        try
        {
            var service = new ImportExportService();
            await service.ExportReportPackAsync(new ReportPackExportModel
            {
                Sections =
                [
                    new ReportPackSectionModel
                    {
                        Title = "Test Bölüm",
                        Headers = ["Kolon"],
                        Rows =
                        [
                            [
                                new ReportPackCellModel { Value = "renkli", BackgroundColor = "#FF92D050" }
                            ],
                            [
                                new ReportPackCellModel { Value = "zebra" }
                            ]
                        ]
                    }
                ]
            }, path);

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 500);
            var header = new byte[4];
            await using (var stream = File.OpenRead(path))
            {
                _ = await stream.ReadAsync(header);
            }

            Assert.Equal("%PDF"u8.ToArray(), header);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
