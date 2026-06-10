using ClosedXML.Excel;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class TadilatExcelImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ReadsSheetsDistrictCarryForwardCommentsAndColors()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "tadilat.xlsx");

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var aktif = workbook.Worksheets.Add("AKTİF");
                WriteHeaders(aktif);
                aktif.Cell(2, 1).Value = "GERZE";
                aktif.Cell(2, 2).Value = "738-8 MUSTAFA KIRMIZI";
                aktif.Cell(2, 3).Value = "MİMARİ";
                aktif.Cell(2, 4).Value = "EVET";
                aktif.Cell(2, 9).Value = "Açıklama satırı";
                aktif.Cell(2, 5).GetComment().AddText("Denetçi notu");
                aktif.Cell(2, 4).Style.Fill.BackgroundColor = XLColor.Red;
                aktif.Cell(2, 6).Style.Fill.BackgroundColor = XLColor.FromIndex(6);

                aktif.Cell(3, 2).Value = "738-8 MUSTAFA KIRMIZI";
                aktif.Cell(3, 3).Value = "STATİK";
                aktif.Cell(3, 4).Value = "MUAF";

                var biten = workbook.Worksheets.Add("GERZE BİTEN");
                WriteHeaders(biten);
                biten.Cell(2, 1).Value = "GERZE";
                biten.Cell(2, 2).Value = "195-1 GÜLNAZ ÇETİNKAYA";
                biten.Cell(2, 3).Value = "MİMARİ";
                biten.Cell(2, 10).Value = "Teslim edildi";

                workbook.SaveAs(filePath);
            }

            var service = new TadilatExcelImportService();
            var result = await service.ImportAsync(filePath);

            Assert.Equal(3, result.Entries.Count);
            Assert.Equal(2, result.Entries.Count(item => item.SubTab == TadilatSubTab.Aktif));
            Assert.Single(result.Entries, item => item.SubTab == TadilatSubTab.Biten);
            Assert.All(result.Entries.Take(2), item => Assert.Equal("GERZE", item.District));

            var noteState = Assert.Single(result.CellStates, item => item.ColumnKey == TadilatColumnKeys.InspectorApproved);
            Assert.Equal("Denetçi notu", noteState.NoteText);

            var colorState = Assert.Single(result.CellStates, item => item.ColumnKey == TadilatColumnKeys.DigitalReceived);
            Assert.False(string.IsNullOrWhiteSpace(colorState.BackgroundColor));
            Assert.Contains(result.CellStates, item => item.ColumnKey == TadilatColumnKeys.OutputAndReportArrived && !string.IsNullOrWhiteSpace(item.BackgroundColor));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static void WriteHeaders(IXLWorksheet worksheet)
    {
        var headers = new[]
        {
            "İLÇE",
            "İŞİN İSMİ",
            "TADİLAT PROJE TÜRÜ",
            "PROJENİN DİJİTALİ GELDİ Mİ ?",
            "PROJEYİ İLGİLİ DENETÇİ ONAYLADI MI?",
            "PROJENİN ÇIKTISI VE TADİLAT RAPORU BÜROYA GELDİ Mİ ?",
            "ÜST YAZI YAZILIP BELEDİYEYE TESLİM EDİLDİ Mİ?",
            "PROJELER BELEDİYEDEN ALINIP ARŞİVE KONULDU MU?",
            "AÇIKLAMA 1",
            "AÇIKLAMA2"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }
    }
}
