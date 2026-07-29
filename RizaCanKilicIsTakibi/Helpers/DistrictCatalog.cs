using System.Globalization;

namespace RizaCanKilicIsTakibi.Helpers;

public static class DistrictCatalog
{
    private const string Merkez = "MERKEZ";
    private const string Sinop = "SİNOP";
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static IReadOnlyList<string> All { get; } =
    [
        "AYANCIK",
        "BOYABAT",
        "BOYABAT OSB",
        "DURAĞAN",
        "ERFELEK",
        "GERZE",
        Merkez,
        "SARAYDÜZÜ",
        Sinop,
        "SİNOP OSB",
        "TÜRKELİ"
    ];

    public static string NormalizeStoredValue(string? district)
        => string.IsNullOrWhiteSpace(district)
            ? string.Empty
            : district.Trim().ToUpper(TurkishCulture);

    public static bool ContainsForFilter(string? storedDistrict, string? query)
    {
        if (SearchTextNormalizer.Contains(storedDistrict, query))
        {
            return true;
        }

        var alias = GetFilterAlias(storedDistrict);
        return alias is not null && SearchTextNormalizer.Contains(alias, query);
    }

    public static string? GetFilterAlias(string? district)
        => NormalizeStoredValue(district) switch
        {
            Merkez => Sinop,
            Sinop => Merkez,
            _ => null
        };

    public static bool AreFilterAliases(string? left, string? right)
    {
        var normalizedLeft = NormalizeStoredValue(left);
        var normalizedRight = NormalizeStoredValue(right);
        if (string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return (normalizedLeft == Merkez && normalizedRight == Sinop)
               || (normalizedLeft == Sinop && normalizedRight == Merkez);
    }
}
