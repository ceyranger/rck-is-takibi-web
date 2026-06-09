using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Helpers;

public static class ContextQueryExplanationBuilder
{
    public static string Build(ContextQueryMatch match)
        => match.MatchType switch
        {
            ContextQueryMatchType.AdaParsel => "Eşleşmeler ada parsel üzerinden bulundu. Bağlı yapı sahibi, müteahhit ve YİBF kayıtları da taranıyor.",
            ContextQueryMatchType.YibfNo => "Eşleşmeler YİBF no üzerinden bulundu. Bağlı ada parsel ve kişi/şirket kayıtları da taranıyor.",
            ContextQueryMatchType.Muteahhit => "Eşleşmeler müteahhit adı üzerinden bulundu. Aynı isme bağlı yapı sahibi ve ada parsel kayıtları da dahil ediliyor.",
            ContextQueryMatchType.YapiSahibi when TokenCount(match.MatchedKey) <= 1 => "Eşleşmeler isim tokenı üzerinden bulundu. Benzersiz isim eşleşmelerinde bağlı ada parsel ve YİBF kayıtları da dahil ediliyor.",
            ContextQueryMatchType.YapiSahibi => "Eşleşmeler yapı sahibi adı üzerinden bulundu. Bağlı ada parsel ve YİBF kayıtları da taranıyor.",
            _ => string.Empty
        };

    private static int TokenCount(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? 0
            : value.Split([' ', '-', '/', ',', '.', ';', ':', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
}
