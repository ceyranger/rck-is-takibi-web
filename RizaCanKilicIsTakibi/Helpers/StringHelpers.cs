namespace RizaCanKilicIsTakibi.Helpers;

public static class StringHelpers
{
    public static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    public static string CombineNonEmpty(params string?[] values)
        => string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
}