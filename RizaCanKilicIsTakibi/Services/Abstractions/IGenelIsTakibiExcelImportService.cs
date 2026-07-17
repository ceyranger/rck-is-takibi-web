using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IGenelIsTakibiExcelImportService
{
    GenelIsTakibiExcelImportResult ImportFromFile(string filePath, string aksiyonaEkleneceklerDistrict = "GENEL");
}
