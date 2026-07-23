namespace RizaCanKilicIsTakibi.Models;

public static class YibfAnaBilgiApprovalStatuses
{
    public const string Incelenecek = "Incelenecek";
    public const string DenetcidenDonus = "DenetcidenDonus";
    public const string MuelliftenRevize = "MuelliftenRevize";
    public const string Beklenen = "Beklenen";
    public const string Onaylanan = "Onaylanan";
    public const string Pasif = "Pasif";

    public const string ColorIncelenecek = "#FFFF0000";
    public const string ColorDenetcidenDonus = "#FFFFA500";
    public const string ColorMuelliftenRevize = "#FFFFFF00";
    public const string ColorBeklenen = "#FFE8E0A8";
    public const string ColorOnaylanan = "#FF92D050";
    public const string ColorPasif = "#FF9E9E9E";
    public const string ColorKategorisiz = "#FFD9D9D9";

    public const string FilterAll = "";
    public const string FilterKategorisiz = "Kategorisiz";

    public static IReadOnlyList<YibfAnaBilgiApprovalStatusOption> DialogOptions { get; } =
    [
        new(string.Empty, "Kategorisiz"),
        new(Incelenecek, "İncelenecek"),
        new(DenetcidenDonus, "Denetçiden dönüş bekleniyor"),
        new(MuelliftenRevize, "Müelliften revize bekleniyor"),
        new(Beklenen, "Beklenen"),
        new(Onaylanan, "Onaylanan"),
        new(Pasif, "Pasif")
    ];

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var trimmed = status.Trim();
        if (string.Equals(trimmed, Incelenecek, StringComparison.OrdinalIgnoreCase))
        {
            return Incelenecek;
        }

        if (string.Equals(trimmed, DenetcidenDonus, StringComparison.OrdinalIgnoreCase))
        {
            return DenetcidenDonus;
        }

        if (string.Equals(trimmed, MuelliftenRevize, StringComparison.OrdinalIgnoreCase))
        {
            return MuelliftenRevize;
        }

        if (string.Equals(trimmed, Beklenen, StringComparison.OrdinalIgnoreCase))
        {
            return Beklenen;
        }

        if (string.Equals(trimmed, Onaylanan, StringComparison.OrdinalIgnoreCase))
        {
            return Onaylanan;
        }

        if (string.Equals(trimmed, Pasif, StringComparison.OrdinalIgnoreCase))
        {
            return Pasif;
        }

        return string.Empty;
    }

    public static string GetLabel(string? status)
        => Normalize(status) switch
        {
            Incelenecek => "İncelenecek",
            DenetcidenDonus => "Denetçiden dönüş bekleniyor",
            MuelliftenRevize => "Müelliften revize bekleniyor",
            Beklenen => "Beklenen",
            Onaylanan => "Onaylanan",
            Pasif => "Pasif",
            _ => "Kategorisiz"
        };

    public static string GetDefaultColorForStatus(string? status)
        => Normalize(status) switch
        {
            Incelenecek => ColorIncelenecek,
            DenetcidenDonus => ColorDenetcidenDonus,
            MuelliftenRevize => ColorMuelliftenRevize,
            Beklenen => ColorBeklenen,
            Onaylanan => ColorOnaylanan,
            Pasif => ColorPasif,
            _ => ColorKategorisiz
        };

    // Kept for callers that still use the old name; returns the default color suggestion.
    public static string? GetColorForStatus(string? status)
        => GetDefaultColorForStatus(status);

    public static string GetFilterKey(string? status)
    {
        var normalized = Normalize(status);
        return string.IsNullOrEmpty(normalized) ? FilterKategorisiz : normalized;
    }

    /// <summary>0 = most urgent. Lower ranks sort first.</summary>
    public static int GetUrgencyRank(string? status)
        => Normalize(status) switch
        {
            Incelenecek => 0,
            DenetcidenDonus => 1,
            MuelliftenRevize => 2,
            Beklenen => 3,
            _ => 4
        };

    public static bool IsApproved(string? status)
        => string.Equals(Normalize(status), Onaylanan, StringComparison.Ordinal);

    public static bool IsPassive(string? status)
        => string.Equals(Normalize(status), Pasif, StringComparison.Ordinal);

    public static bool IsExplicitPending(string? status)
    {
        var normalized = Normalize(status);
        return normalized is Incelenecek or DenetcidenDonus or MuelliftenRevize or Beklenen;
    }
}

public sealed record YibfAnaBilgiApprovalStatusOption(string Value, string Label);
