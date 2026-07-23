namespace RizaCanKilicIsTakibi.Models;

public static class YibfAnaBilgiApprovalStatuses
{
    public const string Incelenecek = "Incelenecek";
    public const string DenetcidenDonus = "DenetcidenDonus";
    public const string MuelliftenRevize = "MuelliftenRevize";
    public const string Onaylanan = "Onaylanan";

    public const string ColorIncelenecek = "#FFFF0000";
    public const string ColorDenetcidenDonus = "#FFFFFF00";
    public const string ColorMuelliftenRevize = "#FFD9D9D9";
    public const string ColorOnaylanan = "#FF92D050";

    public const string FilterAll = "";
    public const string FilterKategorisiz = "Kategorisiz";

    public static IReadOnlyList<YibfAnaBilgiApprovalStatusOption> DialogOptions { get; } =
    [
        new(string.Empty, "Kategorisiz"),
        new(Incelenecek, "İncelenecek"),
        new(DenetcidenDonus, "Denetçiden dönüş bekleniyor"),
        new(MuelliftenRevize, "Müelliften revize bekleniyor"),
        new(Onaylanan, "Onaylanan")
    ];

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var trimmed = status.Trim();
        if (string.Equals(trimmed, Incelenecek, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, DenetcidenDonus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, MuelliftenRevize, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, Onaylanan, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed switch
            {
                _ when string.Equals(trimmed, Incelenecek, StringComparison.OrdinalIgnoreCase) => Incelenecek,
                _ when string.Equals(trimmed, DenetcidenDonus, StringComparison.OrdinalIgnoreCase) => DenetcidenDonus,
                _ when string.Equals(trimmed, MuelliftenRevize, StringComparison.OrdinalIgnoreCase) => MuelliftenRevize,
                _ => Onaylanan
            };
        }

        return string.Empty;
    }

    public static string GetLabel(string? status)
        => Normalize(status) switch
        {
            Incelenecek => "İncelenecek",
            DenetcidenDonus => "Denetçiden dönüş bekleniyor",
            MuelliftenRevize => "Müelliften revize bekleniyor",
            Onaylanan => "Onaylanan",
            _ => "Kategorisiz"
        };

    public static string? GetColorForStatus(string? status)
        => Normalize(status) switch
        {
            Incelenecek => ColorIncelenecek,
            DenetcidenDonus => ColorDenetcidenDonus,
            MuelliftenRevize => ColorMuelliftenRevize,
            Onaylanan => ColorOnaylanan,
            _ => null
        };

    public static string GetFilterKey(string? status)
    {
        var normalized = Normalize(status);
        return string.IsNullOrEmpty(normalized) ? FilterKategorisiz : normalized;
    }

    public static bool IsApproved(string? status)
        => string.Equals(Normalize(status), Onaylanan, StringComparison.Ordinal);

    public static bool IsExplicitPending(string? status)
    {
        var normalized = Normalize(status);
        return normalized is Incelenecek or DenetcidenDonus or MuelliftenRevize;
    }
}

public sealed record YibfAnaBilgiApprovalStatusOption(string Value, string Label);
