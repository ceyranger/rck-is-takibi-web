using RizaCanKilicIsTakibi.Models;
using System.Globalization;
using System.Text;

namespace RizaCanKilicIsTakibi.Services;

public enum YibfWorkIdentityMatchKind
{
    Unmatched,
    ExactBase,
    Variant,
    Ambiguous
}

public sealed record YibfWorkIdentityMatch(
    YibfWorkIdentityMatchKind Kind,
    YibfAnaBilgiEntry? AnaBilgiEntry,
    string VariantLabel);

public static class YibfWorkIdentityService
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static bool NormalizeIdentities(IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries, IReadOnlyList<YibfIsTakibiEntry> isTakibiEntries)
    {
        var changed = false;

        foreach (var entry in anaBilgiEntries)
        {
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
                changed = true;
            }

            if (entry.WorkGroupId == Guid.Empty)
            {
                entry.WorkGroupId = entry.Id;
                changed = true;
            }

            if (entry.WorkIdentityId == Guid.Empty)
            {
                entry.WorkIdentityId = entry.Id;
                changed = true;
            }
        }

        foreach (var entry in isTakibiEntries)
        {
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
                changed = true;
            }

            var match = Classify(entry, anaBilgiEntries);
            // Katalog fan-out / elle bağlanmış kimlikleri koru; eşleşmeyen satırlarda sıfırlama.
            var nextGroupId = entry.WorkGroupId != Guid.Empty ? entry.WorkGroupId : entry.Id;
            var nextIdentityId = entry.WorkIdentityId != Guid.Empty ? entry.WorkIdentityId : entry.Id;
            var nextVariantLabel = entry.WorkVariantLabel ?? string.Empty;

            if (match.Kind == YibfWorkIdentityMatchKind.ExactBase && match.AnaBilgiEntry is not null)
            {
                nextGroupId = match.AnaBilgiEntry.WorkGroupId == Guid.Empty ? match.AnaBilgiEntry.Id : match.AnaBilgiEntry.WorkGroupId;
                nextIdentityId = match.AnaBilgiEntry.WorkIdentityId == Guid.Empty ? match.AnaBilgiEntry.Id : match.AnaBilgiEntry.WorkIdentityId;
                nextVariantLabel = string.Empty;
            }
            else if (match.Kind == YibfWorkIdentityMatchKind.Variant && match.AnaBilgiEntry is not null)
            {
                nextGroupId = match.AnaBilgiEntry.WorkGroupId == Guid.Empty ? match.AnaBilgiEntry.Id : match.AnaBilgiEntry.WorkGroupId;
                nextIdentityId = entry.WorkIdentityId == Guid.Empty || entry.WorkIdentityId == nextGroupId
                    ? entry.Id
                    : entry.WorkIdentityId;
                nextVariantLabel = match.VariantLabel;
            }

            if (entry.WorkGroupId != nextGroupId)
            {
                entry.WorkGroupId = nextGroupId;
                changed = true;
            }

            if (entry.WorkIdentityId != nextIdentityId)
            {
                entry.WorkIdentityId = nextIdentityId;
                changed = true;
            }

            if (!string.Equals(entry.WorkVariantLabel, nextVariantLabel, StringComparison.Ordinal))
            {
                entry.WorkVariantLabel = nextVariantLabel;
                changed = true;
            }
        }

        return changed;
    }

    public static YibfWorkIdentityMatch Classify(YibfIsTakibiEntry isTakibiEntry, IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries)
    {
        var jobKey = NormalizeWorkKey(isTakibiEntry.JobName);
        if (string.IsNullOrWhiteSpace(jobKey))
        {
            return new YibfWorkIdentityMatch(YibfWorkIdentityMatchKind.Unmatched, null, string.Empty);
        }

        var candidates = anaBilgiEntries
            .Select(entry => new Candidate(entry, BuildAnaBilgiBaseKey(entry)))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.BaseKey))
            .Where(candidate => jobKey == candidate.BaseKey || jobKey.StartsWith(candidate.BaseKey + " ", StringComparison.Ordinal))
            .Take(2)
            .ToList();

        if (candidates.Count == 0)
        {
            return new YibfWorkIdentityMatch(YibfWorkIdentityMatchKind.Unmatched, null, string.Empty);
        }

        if (candidates.Count > 1)
        {
            return new YibfWorkIdentityMatch(YibfWorkIdentityMatchKind.Ambiguous, null, string.Empty);
        }

        var candidate = candidates[0];
        if (jobKey == candidate.BaseKey)
        {
            return new YibfWorkIdentityMatch(YibfWorkIdentityMatchKind.ExactBase, candidate.Entry, string.Empty);
        }

        return new YibfWorkIdentityMatch(
            YibfWorkIdentityMatchKind.Variant,
            candidate.Entry,
            jobKey[candidate.BaseKey.Length..].Trim());
    }

    public static string BuildAnaBilgiBaseKey(YibfAnaBilgiEntry entry)
        => NormalizeWorkKey(string.Join(' ', new[] { entry.AdaParsel, entry.YapiSahibi }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())));

    public static string NormalizeWorkKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToUpper(TurkishCulture))
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private sealed record Candidate(YibfAnaBilgiEntry Entry, string BaseKey);
}
