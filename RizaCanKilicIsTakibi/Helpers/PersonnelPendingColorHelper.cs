namespace RizaCanKilicIsTakibi.Helpers;

public static class PersonnelPendingColorHelper
{
    public const string StrongRedColor = "#FFFF0000";
    public const string StrongYellowColor = "#FFFFFF00";
    private const string LegacyPaleRedColor = "#FFF4C4C4";
    private const string LegacyPaleYellowColor = "#FFF7EDB3";

    public static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return string.Empty;
        }

        var trimmed = color.Trim();
        if (string.Equals(trimmed, LegacyPaleRedColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongRedColor;
        }

        if (string.Equals(trimmed, LegacyPaleYellowColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongYellowColor;
        }

        return trimmed.ToUpperInvariant();
    }

    public static bool IsPendingColor(string? color)
    {
        var normalized = NormalizeColor(color);
        return string.Equals(normalized, StrongRedColor, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, StrongYellowColor, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCriticalColor(string? color)
        => string.Equals(NormalizeColor(color), StrongRedColor, StringComparison.OrdinalIgnoreCase);
}
