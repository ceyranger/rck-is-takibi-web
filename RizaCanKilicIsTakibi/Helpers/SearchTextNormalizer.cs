using System.Globalization;
using System.Text;

namespace RizaCanKilicIsTakibi.Helpers;

public static class SearchTextNormalizer
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lowered = value.Trim().ToLower(TurkishCulture);
        var normalized = lowered
            .Replace('ı', 'i')
            .Replace('İ', 'i');

        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool Contains(string? source, string? query)
    {
        var normalizedQuery = Normalize(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        return Normalize(source).Contains(normalizedQuery, StringComparison.Ordinal);
    }

    public static bool StartsWith(string? source, string? query)
    {
        var normalizedQuery = Normalize(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        return Normalize(source).StartsWith(normalizedQuery, StringComparison.Ordinal);
    }

    public static bool EqualsNormalized(string? left, string? right)
        => string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
}
