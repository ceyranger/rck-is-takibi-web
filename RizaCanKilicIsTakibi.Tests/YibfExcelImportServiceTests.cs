using ClosedXML.Excel;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class YibfExcelImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ReadsAnaBilgiBlocksAndIsTakibiCellStates()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "yibf.xlsx");

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var ana = workbook.Worksheets.Add("ANA BİLGİ");
                ana.Cell(1, 1).Value = "ADA PARSEL";
                ana.Cell(1, 2).Value = "İŞ BİLGİLERİ";
                ana.Cell(1, 3).Value = "OLAYLAR";
                ana.Cell(2, 1).Value = "235-1";
                ana.Cell(2, 2).Value = "YİBF NO: 1855397";
                ana.Cell(3, 2).Value = "İL ÖZEL İDARESİ";
                ana.Cell(4, 2).Value = "ORSA ENERJİ";
                ana.Cell(5, 2).Value = "SEKVAN İNŞAAT";
                ana.Cell(2, 3).Value = new DateTime(2021, 12, 9);
                ana.Cell(3, 3).Value = "RUHSAT AŞAMASINDA EKSİK EVRAKLAR GELDİ";
                ana.Cell(3, 3).Style.Fill.BackgroundColor = XLColor.Yellow;
                ana.Cell(3, 3).GetComment().AddText("Ana bilgi notu");

                var isTakibi = workbook.Worksheets.Add("İŞ TAKİBİ");
                var headers = new[]
                {
                    "İŞİN İSMİ",
                    "MÜELLİF BİLGİLERİ GELDİ Mİ?",
                    "DENETÇİ ATAMALARI 10 GÜN İÇİNDE YAPILDI MI?",
                    "TÜM PROJELERİN DİJİTALİ VAR MI?",
                    "EVRAKLAR TAM MI?",
                    "YİBF SÖZLEŞME TAAHHÜTNAME HAZIRLANDI MI? İMZALAR TAM MI?",
                    "DEKONT ALINDI MI?",
                    "RUHSAT BAŞVURUSU YAPILDI MI?",
                    "RUHSAT NÜSHASI ALINDI MI?",
                    "İŞYERİ TESLİM TUTANAĞI HAZIRLANDI MI?",
                    "İSG YAZISI HAZIRLANDI MI? İMZALAR TAM MI?",
                    "SAĞLIK GÜVENLİK PLANI GELDİ Mİ ?",
                    "TEMEL TOPRAKLAMA TUTANAĞI HAZIRLANDI MI?"
                };

                for (var index = 0; index < headers.Length; index++)
                {
                    isTakibi.Cell(1, index + 1).Value = headers[index];
                }

                isTakibi.Cell(2, 1).Value = "235-1 ORSA ENERJİ";
                isTakibi.Cell(2, 2).Value = "EVET";
                isTakibi.Cell(2, 3).Value = "EVET";
                isTakibi.Cell(2, 4).Value = "HAYIR";
                isTakibi.Cell(2, 4).Style.Fill.BackgroundColor = XLColor.Red;
                isTakibi.Cell(2, 4).GetComment().AddText("Dijital eksik");

                workbook.SaveAs(filePath);
            }

            var service = new YibfExcelImportService();
            var result = await service.ImportAsync(filePath);

            Assert.Single(result.AnaBilgiEntries);
            Assert.Single(result.AnaBilgiEvents);
            Assert.Equal("235-1", result.AnaBilgiEntries[0].AdaParsel);
            Assert.Equal("1855397", result.AnaBilgiEntries[0].YibfNo);
            Assert.Equal("İL ÖZEL İDARESİ", result.AnaBilgiEntries[0].Idare);
            Assert.Equal("ORSA ENERJİ", result.AnaBilgiEntries[0].YapiSahibi);
            Assert.Equal("SEKVAN İNŞAAT", result.AnaBilgiEntries[0].Muteahhit);
            Assert.Equal("Ana bilgi notu", result.AnaBilgiEvents[0].NoteText);
            Assert.False(string.IsNullOrWhiteSpace(result.AnaBilgiEvents[0].BackgroundColor));

            Assert.Single(result.IsTakibiEntries);
            Assert.Single(result.CellStates);
            Assert.Equal("235-1 ORSA ENERJİ", result.IsTakibiEntries[0].JobName);
            Assert.Equal("Dijital eksik", result.CellStates[0].NoteText);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
