using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services;

public static class CrashRecoverySummaryBuilder
{
    public static IReadOnlyList<string> Build(BackupRestoreData recovery, BackupRestoreData current)
    {
        var lines = new List<string>();

        AddCountLine(lines, "Genel / Acil işler", recovery.Tasks.Count, current.Tasks.Count, SampleTitles(recovery.Tasks.Select(t => t.Title), current.Tasks.Select(t => t.Title)));
        AddCountLine(lines, "Aksiyon kayıtları", recovery.ActionEntries.Count, current.ActionEntries.Count);
        AddCountLine(lines, "Eksik proje", recovery.MissingProjectEntries.Count, current.MissingProjectEntries.Count);
        AddCountLine(lines, "Karot", recovery.KarotEntries.Count, current.KarotEntries.Count);
        AddCountLine(lines, "Tadilat", recovery.TadilatEntries.Count, current.TadilatEntries.Count);
        AddCountLine(
            lines,
            "YİBF Ana Bilgi kayıt",
            recovery.YibfAnaBilgiEntries.Count,
            current.YibfAnaBilgiEntries.Count,
            SampleTitles(recovery.YibfAnaBilgiEntries.Select(e => e.AdaParsel), current.YibfAnaBilgiEntries.Select(e => e.AdaParsel)));
        AddCountLine(
            lines,
            "YİBF Ana Bilgi olay",
            recovery.YibfAnaBilgiEvents.Count,
            current.YibfAnaBilgiEvents.Count,
            SampleTitles(recovery.YibfAnaBilgiEvents.Select(e => e.Description), current.YibfAnaBilgiEvents.Select(e => e.Description)));
        AddCountLine(lines, "YİBF İş Takibi", recovery.YibfIsTakibiEntries.Count, current.YibfIsTakibiEntries.Count);
        AddCountLine(lines, "Proje kataloğu", recovery.ProjectCatalogEntries.Count, current.ProjectCatalogEntries.Count);

        if (lines.Count == 0)
        {
            lines.Add("Kaydedilmemiş oturum anlık görüntüsü bulundu (adet farkı görünmüyor; içerik değişmiş olabilir).");
        }

        return lines;
    }

    private static void AddCountLine(
        ICollection<string> lines,
        string label,
        int recoveryCount,
        int currentCount,
        string? samples = null)
    {
        if (recoveryCount == currentCount && string.IsNullOrWhiteSpace(samples))
        {
            return;
        }

        var delta = recoveryCount - currentCount;
        var deltaText = delta == 0 ? "adet aynı" : delta > 0 ? $"+{delta}" : delta.ToString();
        var line = $"{label}: kurtarma {recoveryCount}, kayıtlı {currentCount} ({deltaText})";
        if (!string.IsNullOrWhiteSpace(samples))
        {
            line += $" — örn: {samples}";
        }

        lines.Add(line);
    }

    private static string? SampleTitles(IEnumerable<string?> recoveryTitles, IEnumerable<string?> currentTitles)
    {
        var current = new HashSet<string>(
            currentTitles.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()),
            StringComparer.OrdinalIgnoreCase);
        var added = recoveryTitles
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Where(t => !current.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        return added.Count == 0 ? null : string.Join(", ", added);
    }
}
