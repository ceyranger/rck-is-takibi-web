namespace RizaCanKilicIsTakibi.Helpers;

internal static class SqliteGuidHelper
{
    public static Guid? ParseNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string ToDb(Guid? value)
        => value?.ToString("D") ?? string.Empty;
}
