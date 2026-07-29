using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ContextInsightBuilder : IContextInsightBuilder
{
    private static readonly string[] SectionOrder =
    [
        "Eksik Proje",
        "Karot",
        "Proje Takibi",
        "YİBF İş Takibi",
        "Tadilat",
        "Aksiyon",
        "Genel İş Takibi"
    ];

    private const string StrongRedColor = "#FFFF0000";
    private const string StrongYellowColor = "#FFFFFF00";
    private const string LegacyPaleRedColor = "#FFF4C4C4";
    private const string LegacyPaleYellowColor = "#FFF7EDB3";

    private readonly ISearchService _searchService;

    public ContextInsightBuilder(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public QueryInsightResult Build(
        ContextQueryMatch match,
        IReadOnlyList<SearchResultItem> corpus,
        IEnumerable<TaskItem> tasks,
        IEnumerable<ActionEntry> actionEntries,
        IEnumerable<MissingProjectEntry> missingProjectEntries,
        IEnumerable<KarotEntry> karotEntries,
        IEnumerable<TadilatEntry> aktifTadilatEntries,
        IEnumerable<TadilatCellState> tadilatCellStates,
        IEnumerable<YibfAnaBilgiEntry> yibfAnaBilgiEntries,
        IEnumerable<YibfAnaBilgiEvent> yibfAnaBilgiEvents,
        IEnumerable<YibfIsTakibiEntry> yibfIsTakibiEntries,
        IEnumerable<YibfCellState> yibfCellStates)
    {
        if (match is null || !match.HasMatch)
        {
            return new QueryInsightResult
            {
                AnswerText = "Sorgudan aranacak anahtar çıkarılamadı. Ada parsel, yapı sahibi, müteahhit veya YİBF no ile tekrar deneyin."
            };
        }

        var matchedRecords = corpus
            .Where(item => MatchesSearchItem(match, item))
            .ToList();

        if (matchedRecords.Count == 0)
        {
            return new QueryInsightResult
            {
                MatchedKey = match.MatchedKey,
                AnswerText = $"{match.MatchedKey} için kayıt bulunamadı."
            };
        }

        var insights = new List<string>();
        var sectionItems = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
        var sourceRoleLabels = new Dictionary<string, string>(StringComparer.Ordinal);
        var sourceLookup = corpus.ToDictionary(BuildSearchResultKey, StringComparer.Ordinal);

        if (match.IntentType != ContextQueryIntentType.CompletedOnly)
        {
            AppendGeneralContextInsights(insights, sectionItems, sourceKeys, sourceLookup, sourceRoleLabels, match, tasks);
            AppendActionContextInsights(insights, sectionItems, sourceKeys, sourceLookup, sourceRoleLabels, match, actionEntries);
            AppendMissingProjectContextInsights(insights, sectionItems, sourceKeys, sourceLookup, sourceRoleLabels, match, missingProjectEntries);
        }

        AppendKarotContextInsights(insights, sectionItems, sourceKeys, sourceLookup, sourceRoleLabels, match, karotEntries);
        if (match.IntentType != ContextQueryIntentType.CompletedOnly)
        {
            AppendTadilatContextInsights(insights, sectionItems, sourceKeys, sourceLookup, sourceRoleLabels, match, aktifTadilatEntries, tadilatCellStates);
            AppendYibfAnaBilgiContextInsights(insights, sectionItems, sourceKeys, sourceLookup, sourceRoleLabels, match, yibfAnaBilgiEntries, yibfAnaBilgiEvents);
            AppendYibfIsTakibiContextInsights(insights, sectionItems, sourceKeys, sourceLookup, sourceRoleLabels, match, yibfIsTakibiEntries, yibfCellStates);
        }

        var filteredSources = corpus
            .Where(item => sourceKeys.Contains(BuildSearchResultKey(item)))
            .ToList();

        var orderedSources = filteredSources.Count == 0
            ? Array.Empty<SearchResultItem>()
            : _searchService.SearchAll(filteredSources, match.MatchedKey, SearchScope.All)
                .Select(item => EnrichSource(item, match, sourceRoleLabels))
                .ToArray();

        var summary = BuildAnswerText(match, insights);
        var sections = BuildSections(sectionItems);

        return new QueryInsightResult
        {
            MatchedKey = match.MatchedKey,
            SummaryText = summary,
            AnswerText = summary,
            Sections = sections,
            Sources = orderedSources
        };
    }

    private static IReadOnlyList<QueryInsightSection> BuildSections(IReadOnlyDictionary<string, List<string>> sectionItems)
        => SectionOrder
            .Where(sectionItems.ContainsKey)
            .Select(title => new QueryInsightSection
            {
                Title = title,
                Items = sectionItems[title],
                SourceCount = sectionItems[title].Count
            })
            .Where(section => section.Items.Count > 0)
            .ToArray();

    private static string BuildAnswerText(ContextQueryMatch match, IReadOnlyCollection<string> insights)
    {
        if (insights.Count == 0)
        {
            return match.IntentType switch
            {
                ContextQueryIntentType.CompletedOnly => $"{match.MatchedKey} için tamamlanan kayıt görünmüyor.",
                ContextQueryIntentType.PendingOnly => $"{match.MatchedKey} için bekleyen konu görünmüyor.",
                _ => $"{match.MatchedKey} için kayıt bulundu, ancak açık eksik veya bekleyen konu görünmüyor."
            };
        }

        var intro = match.IntentType switch
        {
            ContextQueryIntentType.CompletedOnly => $"{match.MatchedKey} için {insights.Count} tamamlanan kayıt bulundu.",
            ContextQueryIntentType.PendingOnly => $"{match.MatchedKey} için {insights.Count} bekleyen konu bulundu.",
            _ => $"{match.MatchedKey} için {insights.Count} açık konu bulundu."
        };

        if (match.PrimaryRole == ContextQueryRole.Muteahhit)
        {
            intro = $"{match.MatchedKey} için {insights.Count} ilgili kayıt bulundu. Müteahhit ve aynı isimle eşleşen yapı sahibi kayıtları birlikte gösteriliyor.";
        }

        return $"{intro}{Environment.NewLine}{string.Join(Environment.NewLine, insights.Select(item => $"• {item}"))}";
    }

    private static void AppendGeneralContextInsights(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        ContextQueryMatch match,
        IEnumerable<TaskItem> tasks)
    {
        if (match.MatchType == ContextQueryMatchType.Muteahhit)
        {
            return;
        }

        foreach (var task in tasks.Where(task => MatchesGeneralTask(match, task, sourceLookup)))
        {
            var summary = FirstNonEmpty(task.Title, task.Notes.FirstOrDefault()?.Text, task.Description, "(Başlıksız görev)");
            AddInsight(insights, sectionItems, "Genel İş Takibi", summary);
            AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.GeneralTask, task.Id);
        }
    }

    private static void AppendActionContextInsights(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        ContextQueryMatch match,
        IEnumerable<ActionEntry> actionEntries)
    {
        if (match.MatchType == ContextQueryMatchType.Muteahhit)
        {
            foreach (var entry in actionEntries.Where(entry => MatchesAnyAllowedRole(match, null, entry.OwnerParcelText)))
            {
                var summary = FirstNonEmpty(entry.OwnerParcelText, entry.WorkText, "(Boş aksiyon kaydı)");
                var workText = FirstNonEmpty(entry.WorkText, "-");
                var roleLabel = BuildRoleLabel(match, null, entry.OwnerParcelText);
                AddInsight(insights, sectionItems, "Aksiyon", $"{summary} - {workText}{roleLabel}");
                AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.ActionEntry, entry.Id, roleLabel);
            }

            return;
        }

        foreach (var entry in actionEntries.Where(entry => Matches(match, entry.District, entry.OwnerParcelText, entry.WorkText)))
        {
            var summary = FirstNonEmpty(entry.OwnerParcelText, entry.WorkText, "(Boş aksiyon kaydı)");
            var workText = FirstNonEmpty(entry.WorkText, "-");
            AddInsight(insights, sectionItems, "Aksiyon", $"{summary} - {workText}");
            AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.ActionEntry, entry.Id);
        }
    }

    private static void AppendMissingProjectContextInsights(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        ContextQueryMatch match,
        IEnumerable<MissingProjectEntry> missingProjectEntries)
    {
        if (match.MatchType == ContextQueryMatchType.Muteahhit)
        {
            foreach (var entry in missingProjectEntries.Where(entry => MatchesAnyAllowedRole(match, null, entry.YapiSahibi)))
            {
                var ownerParcel = BuildOwnerParcelSummary(entry.AdaParsel, entry.YapiSahibi);
                var missingProjectText = FirstNonEmpty(entry.MissingProjectText, entry.Description, "-");
                var roleLabel = BuildRoleLabel(match, null, entry.YapiSahibi);
                AddInsight(insights, sectionItems, "Eksik Proje", $"{ownerParcel} - {missingProjectText}{roleLabel}");
                AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.MissingProjectEntry, entry.Id, roleLabel);
            }

            return;
        }

        foreach (var entry in missingProjectEntries.Where(entry => Matches(match, entry.AdaParsel, entry.YapiSahibi, entry.MissingProjectText, entry.Description)))
        {
            var ownerParcel = BuildOwnerParcelSummary(entry.AdaParsel, entry.YapiSahibi);
            var missingProjectText = FirstNonEmpty(entry.MissingProjectText, entry.Description, "-");
            AddInsight(insights, sectionItems, "Eksik Proje", $"{ownerParcel} - {missingProjectText}");
            AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.MissingProjectEntry, entry.Id);
        }
    }

    private static void AppendKarotContextInsights(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        ContextQueryMatch match,
        IEnumerable<KarotEntry> karotEntries)
    {
        foreach (var entry in karotEntries.Where(entry =>
                     ShouldIncludeKarotEntry(match, entry) &&
                     MatchesKarotEntry(match, entry)))
        {
            var ownerParcel = BuildOwnerParcelSummary(entry.AdaParsel, entry.YapiSahibi);
            var floorInfo = FirstNonEmpty(entry.KatBilgisi, "-");
            var statusText = entry.Status switch
            {
                KarotStatus.KarotAlindiOlumlu => "KAROT OLUMLU",
                KarotStatus.KarotAlindiOlumsuz => "KAROT OLUMSUZ",
                KarotStatus.KarotAlindiSonucBekleniyor => "SONUÇ BEKLENİYOR",
                _ => "KAROT ALINACAK"
            };

            var roleLabel = BuildRoleLabel(match, entry.Muteahhit, entry.YapiSahibi);
            AddInsight(insights, sectionItems, "Karot", $"{ownerParcel} - {floorInfo} - {statusText}{roleLabel}");
            AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.KarotEntry, entry.Id, roleLabel);
        }
    }

    private static bool ShouldIncludeKarotEntry(ContextQueryMatch match, KarotEntry entry)
        => match.IntentType switch
        {
            ContextQueryIntentType.CompletedOnly => entry.Status == KarotStatus.KarotAlindiOlumlu,
            ContextQueryIntentType.PendingOnly => entry.Status is KarotStatus.KarotAlinacak or KarotStatus.KarotAlindiSonucBekleniyor,
            _ => entry.Status != KarotStatus.KarotAlindiOlumlu
        };

    private static void AppendTadilatContextInsights(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        ContextQueryMatch match,
        IEnumerable<TadilatEntry> aktifTadilatEntries,
        IEnumerable<TadilatCellState> tadilatCellStates)
    {
        var activeEntriesById = aktifTadilatEntries.ToDictionary(entry => entry.Id);
        var groupedStates = tadilatCellStates
            .Where(state => IsPendingSummaryColor(state.BackgroundColor))
            .GroupBy(state => state.EntryId);

        if (match.MatchType == ContextQueryMatchType.Muteahhit)
        {
            foreach (var stateGroup in groupedStates)
            {
                if (!activeEntriesById.TryGetValue(stateGroup.Key, out var entry) ||
                    !MatchesAnyAllowedRole(match, null, entry.JobName))
                {
                    continue;
                }

                var reasons = stateGroup
                    .OrderBy(state => GetTadilatColumnOrder(state.ColumnKey))
                    .ThenBy(state => state.ColumnKey, StringComparer.OrdinalIgnoreCase)
                    .Select(state => BuildTadilatSummaryReason(state.ColumnKey))
                    .Where(reason => !string.IsNullOrWhiteSpace(reason))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (reasons.Count == 0)
                {
                    continue;
                }

                var roleLabel = BuildRoleLabel(match, null, entry.JobName);
                AddInsight(sectionItems: sectionItems, insights: insights, sectionTitle: "Tadilat", item: $"{FirstNonEmpty(entry.JobName, entry.ProjectType, "(Boş tadilat kaydı)")} - {string.Join(" VE ", reasons)}{roleLabel}");
                AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.TadilatEntry, entry.Id, roleLabel);
            }

            return;
        }

        foreach (var stateGroup in groupedStates)
        {
            if (!activeEntriesById.TryGetValue(stateGroup.Key, out var entry) ||
                !Matches(match, entry.District, entry.JobName, entry.ProjectType, entry.Description1, entry.Description2))
            {
                continue;
            }

            var reasons = stateGroup
                .OrderBy(state => GetTadilatColumnOrder(state.ColumnKey))
                .ThenBy(state => state.ColumnKey, StringComparer.OrdinalIgnoreCase)
                .Select(state => BuildTadilatSummaryReason(state.ColumnKey))
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (reasons.Count == 0)
            {
                continue;
            }

            AddInsight(sectionItems: sectionItems, insights: insights, sectionTitle: "Tadilat", item: $"{FirstNonEmpty(entry.JobName, entry.ProjectType, "(Boş tadilat kaydı)")} - {string.Join(" VE ", reasons)}");
            AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.TadilatEntry, entry.Id);
        }
    }

    private static void AppendYibfAnaBilgiContextInsights(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        ContextQueryMatch match,
        IEnumerable<YibfAnaBilgiEntry> yibfAnaBilgiEntries,
        IEnumerable<YibfAnaBilgiEvent> yibfAnaBilgiEvents)
    {
        var entryLookup = yibfAnaBilgiEntries.ToDictionary(entry => entry.Id);
        var pendingEvents = yibfAnaBilgiEvents
            .Where(evt => IsPendingSummaryColor(evt.BackgroundColor) && !string.IsNullOrWhiteSpace(evt.Description));

        foreach (var evt in pendingEvents)
        {
            if (!entryLookup.TryGetValue(evt.EntryId, out var entry) ||
                !MatchesYibfAnaBilgi(match, entry, evt))
            {
                continue;
            }

            var ownerParcel = BuildOwnerParcelSummary(entry.AdaParsel, entry.YapiSahibi);
            var roleLabel = BuildRoleLabel(match, entry.Muteahhit, entry.YapiSahibi);
            AddInsight(insights, sectionItems, "Proje Takibi", $"{ownerParcel} - {FirstNonEmpty(evt.Description, evt.NoteText, "-")}{roleLabel}");
            AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.YibfAnaBilgiEvent, evt.Id, roleLabel);
        }
    }

    private static void AppendYibfIsTakibiContextInsights(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        ContextQueryMatch match,
        IEnumerable<YibfIsTakibiEntry> yibfIsTakibiEntries,
        IEnumerable<YibfCellState> yibfCellStates)
    {
        var entriesById = yibfIsTakibiEntries.ToDictionary(entry => entry.Id);
        var groupedStates = yibfCellStates
            .Where(state => IsPendingSummaryColor(state.BackgroundColor))
            .GroupBy(state => state.EntryId);

        if (match.MatchType == ContextQueryMatchType.Muteahhit)
        {
            foreach (var stateGroup in groupedStates)
            {
                if (!entriesById.TryGetValue(stateGroup.Key, out var entry) ||
                    !MatchesAnyAllowedRole(match, null, entry.JobName))
                {
                    continue;
                }

                var orderedStates = stateGroup
                    .OrderBy(state => GetYibfIsTakibiColumnOrder(state.ColumnKey))
                    .ThenBy(state => state.ColumnKey, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var reasons = new List<string>();
                var reasonLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var state in orderedStates)
                {
                    var reason = BuildYibfIsTakibiSummaryReason(state.ColumnKey);
                    if (!string.IsNullOrWhiteSpace(reason) && reasonLookup.Add(reason))
                    {
                        reasons.Add(reason);
                    }
                }

                if (reasons.Count == 0)
                {
                    continue;
                }

                var roleLabel = BuildRoleLabel(match, null, entry.JobName);
                AddInsight(insights, sectionItems, "YİBF İş Takibi", $"{FirstNonEmpty(entry.JobName, "(Boş YİBF iş takibi kaydı)")} - {string.Join(" VE ", reasons)}{roleLabel}");
                AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.YibfIsTakibiEntry, entry.Id, roleLabel);
            }

            return;
        }

        foreach (var stateGroup in groupedStates)
        {
            if (!entriesById.TryGetValue(stateGroup.Key, out var entry) ||
                !Matches(
                    match,
                    entry.JobName,
                    entry.MuellifBilgileriGeldiMi,
                    entry.DenetciAtamalariYapildiMi,
                    entry.TumProjelerinDijitaliVarMi,
                    entry.EvraklarTamMi,
                    entry.YibfSozlesmeHazirlandiMi,
                    entry.DekontAlindiMi,
                    entry.RuhsatBasvurusuYapildiMi,
                    entry.RuhsatNushasiAlindiMi,
                    entry.IsyeriTeslimTutangiHazirlandiMi,
                    entry.IsgYazisiHazirlandiMi,
                    entry.SaglikGuvenlikPlaniGeldiMi,
                    entry.TemelTopraklamaTutanagiHazirlandiMi))
            {
                continue;
            }

            var orderedStates = stateGroup
                .OrderBy(state => GetYibfIsTakibiColumnOrder(state.ColumnKey))
                .ThenBy(state => state.ColumnKey, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var reasons = new List<string>();
            var reasonLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var state in orderedStates)
            {
                var reason = BuildYibfIsTakibiSummaryReason(state.ColumnKey);
                if (!string.IsNullOrWhiteSpace(reason) && reasonLookup.Add(reason))
                {
                    reasons.Add(reason);
                }
            }

            if (reasons.Count == 0)
            {
                continue;
            }

            AddInsight(insights, sectionItems, "YİBF İş Takibi", $"{FirstNonEmpty(entry.JobName, "(Boş YİBF iş takibi kaydı)")} - {string.Join(" VE ", reasons)}");
            AddSource(sourceKeys, sourceLookup, sourceRoleLabels, SearchResultKind.YibfIsTakibiEntry, entry.Id);
        }
    }

    private static void AddInsight(
        ICollection<string> insights,
        IDictionary<string, List<string>> sectionItems,
        string sectionTitle,
        string item)
    {
        if (!sectionItems.TryGetValue(sectionTitle, out var items))
        {
            items = [];
            sectionItems[sectionTitle] = items;
        }

        items.Add(item);
        insights.Add($"{sectionTitle}: {item}");
    }

    private static bool MatchesSearchItem(ContextQueryMatch match, SearchResultItem item)
        => Matches(match, item.Title, item.Summary, item.SearchText);

    private static bool MatchesGeneralTask(
        ContextQueryMatch match,
        TaskItem task,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup)
    {
        if (Matches(match, task.Title, task.Description, string.Join(' ', task.Notes.Select(note => note.Text))))
        {
            return true;
        }

        var key = BuildSearchResultKey(SearchResultKind.GeneralTask, task.Id);
        return sourceLookup.TryGetValue(key, out var item) && MatchesSearchItem(match, item);
    }

    private static bool MatchesKarotEntry(ContextQueryMatch match, KarotEntry entry)
        => match.MatchType switch
        {
            ContextQueryMatchType.Muteahhit => MatchesAnyAllowedRole(match, entry.Muteahhit, entry.YapiSahibi),
            ContextQueryMatchType.YapiSahibi when match.PrimaryRole == ContextQueryRole.YapiSahibi => Matches(match, entry.YapiSahibi, entry.AdaParsel, entry.YibfNo, entry.KatBilgisi, entry.Aciklama),
            _ => Matches(match, entry.YibfNo, entry.AdaParsel, entry.YapiSahibi, entry.Muteahhit, entry.KatBilgisi, entry.Aciklama)
        };

    private static bool MatchesYibfAnaBilgi(ContextQueryMatch match, YibfAnaBilgiEntry entry, YibfAnaBilgiEvent evt)
        => match.MatchType switch
        {
            ContextQueryMatchType.Muteahhit => MatchesAnyAllowedRole(match, entry.Muteahhit, entry.YapiSahibi),
            ContextQueryMatchType.YapiSahibi when match.PrimaryRole == ContextQueryRole.YapiSahibi => Matches(match, entry.YapiSahibi, entry.AdaParsel, entry.YibfNo, evt.Description, evt.NoteText),
            _ => Matches(match, entry.AdaParsel, entry.YapiSahibi, entry.Muteahhit, entry.YibfNo, evt.Description, evt.NoteText)
        };

    private static bool MatchesAnyAllowedRole(ContextQueryMatch match, string? muteahhitValue, string? yapiSahibiValue)
    {
        if (match.MatchType != ContextQueryMatchType.Muteahhit || match.AllowedRoles.Count == 0)
        {
            return false;
        }

        return match.AllowedRoles.Any(role => role switch
        {
            ContextQueryRole.Muteahhit => MatchValue(match, muteahhitValue),
            ContextQueryRole.YapiSahibi => MatchValue(match, yapiSahibiValue),
            _ => false
        });
    }

    private static string BuildRoleLabel(ContextQueryMatch match, string? muteahhitValue, string? yapiSahibiValue)
    {
        if (match.MatchType != ContextQueryMatchType.Muteahhit || match.AllowedRoles.Count == 0)
        {
            return string.Empty;
        }

        var labels = new List<string>();
        if (match.AllowedRoles.Contains(ContextQueryRole.Muteahhit) && MatchValue(match, muteahhitValue))
        {
            labels.Add("MÜTEAHHİT");
        }

        if (match.AllowedRoles.Contains(ContextQueryRole.YapiSahibi) && MatchValue(match, yapiSahibiValue))
        {
            labels.Add("YAPI SAHİBİ");
        }

        return labels.Count == 0 ? string.Empty : $" [{string.Join(" / ", labels)}]";
    }

    private static bool Matches(ContextQueryMatch match, params string?[] values)
        => values.Any(value => MatchValue(match, value));

    private static bool MatchValue(ContextQueryMatch match, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(match.NormalizedKey))
        {
            return false;
        }

        return match.MatchType switch
        {
            ContextQueryMatchType.AdaParsel => ContainsExactToken(value, match.MatchedKey),
            ContextQueryMatchType.YibfNo => ContainsExactToken(value, match.MatchedKey) || SearchTextNormalizer.StartsWith(value, match.MatchedKey),
            ContextQueryMatchType.YapiSahibi or ContextQueryMatchType.Muteahhit => ContainsNameMatch(value, match),
            _ => SearchTextNormalizer.Contains(value, match.MatchedKey)
        };
    }

    private static bool ContainsExactToken(string source, string key)
    {
        var normalizedSource = SearchTextNormalizer.Normalize(source);
        var normalizedKey = SearchTextNormalizer.Normalize(key);
        if (string.IsNullOrWhiteSpace(normalizedSource) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            return false;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(
            normalizedSource,
            $@"(?<![a-z0-9]){System.Text.RegularExpressions.Regex.Escape(normalizedKey)}(?![a-z0-9])",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    private static bool ContainsNameMatch(string source, ContextQueryMatch match)
    {
        var sourceTokens = Tokenize(SearchTextNormalizer.Normalize(source));
        var queryTokens = Tokenize(match.NormalizedKey);
        if (sourceTokens.Length == 0 || queryTokens.Length == 0)
        {
            return false;
        }

        return queryTokens.All(queryToken =>
            queryToken.Length >= 3 &&
            sourceTokens.Any(sourceToken => sourceToken.StartsWith(queryToken, StringComparison.Ordinal)));
    }

    private static string[] Tokenize(string value)
        => value
            .Split([' ', '-', '/', ',', '.', ';', ':', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void AddSource(
        ISet<string> sourceKeys,
        IReadOnlyDictionary<string, SearchResultItem> sourceLookup,
        IDictionary<string, string> sourceRoleLabels,
        SearchResultKind kind,
        Guid itemId,
        string? roleLabel = null)
    {
        var key = BuildSearchResultKey(kind, itemId);
        if (sourceLookup.ContainsKey(key))
        {
            sourceKeys.Add(key);
            if (!string.IsNullOrWhiteSpace(roleLabel))
            {
                sourceRoleLabels[key] = roleLabel.Trim();
            }
        }
    }

    private static SearchResultItem EnrichSource(SearchResultItem item, ContextQueryMatch match, IReadOnlyDictionary<string, string> sourceRoleLabels)
    {
        var key = BuildSearchResultKey(item);
        sourceRoleLabels.TryGetValue(key, out var roleLabel);

        var directMatch = Matches(match, item.Title, item.Summary, item.RawSearchText);
        var matchOriginLabel = directMatch ? "DOĞRUDAN" : "BAĞLAM";
        var summary = item.Summary;

        if (!string.IsNullOrWhiteSpace(roleLabel) &&
            !summary.Contains(roleLabel, StringComparison.OrdinalIgnoreCase))
        {
            summary = $"{summary} {roleLabel}".Trim();
        }

        return new SearchResultItem
        {
            Kind = item.Kind,
            TargetTab = item.TargetTab,
            ItemId = item.ItemId,
            ParentItemId = item.ParentItemId,
            BoardType = item.BoardType,
            BoardLabel = item.BoardLabel,
            Title = item.Title,
            Summary = summary,
            SearchText = item.SearchText,
            RawSearchText = item.RawSearchText,
            MatchOriginLabel = matchOriginLabel
        };
    }

    private static string BuildSearchResultKey(SearchResultItem item)
        => BuildSearchResultKey(item.Kind, item.ItemId);

    private static string BuildSearchResultKey(SearchResultKind kind, Guid itemId)
        => $"{kind}:{itemId:N}";

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string BuildOwnerParcelSummary(string? adaParsel, string? yapiSahibi)
    {
        var parcel = adaParsel?.Trim();
        var owner = yapiSahibi?.Trim();
        if (!string.IsNullOrWhiteSpace(parcel) && !string.IsNullOrWhiteSpace(owner))
        {
            return $"{parcel} + {owner}";
        }

        return FirstNonEmpty(parcel, owner);
    }

    private static string BuildTadilatSummaryReason(string? columnKey)
        => columnKey switch
        {
            TadilatColumnKeys.DigitalReceived => "DİJİTAL GELMEDİ",
            TadilatColumnKeys.InspectorApproved => "DENETÇİ ONAYLAMADI",
            TadilatColumnKeys.OutputAndReportArrived => "ÇIKTI/RAPOR GELMEDİ",
            TadilatColumnKeys.OfficialLetterSubmitted => "ÜST YAZI TESLİM EDİLMEDİ",
            TadilatColumnKeys.ArchivedFromMunicipality => "PROJELER ARŞİVE EKLENMEDİ",
            _ => BuildFallbackSummaryReason(columnKey)
        };

    private static string BuildYibfIsTakibiSummaryReason(string? columnKey)
        => columnKey switch
        {
            YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi => "MÜELLİF BİLGİLERİ GELMEDİ",
            YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi => "DENETÇİ ATAMALARI YAPILMADI",
            YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi => "TÜM PROJELERİN DİJİTALİ YOK",
            YibfIsTakibiColumnKeys.EvraklarTamMi => "EVRAKLAR TAM DEĞİL",
            YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi => "YİBF SÖZLEŞME/TAAHHÜTNAME HAZIR DEĞİL",
            YibfIsTakibiColumnKeys.DekontAlindiMi => "DEKONT ALINMADI",
            YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi => "RUHSAT BAŞVURUSU YAPILMADI",
            YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi => "RUHSAT NÜSHASI ALINMADI",
            YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi => "İŞYERİ TESLİM TUTANAĞI HAZIRLANMADI",
            YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi => "İSG YAZISI HAZIRLANMADI",
            YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi => "SAĞLIK GÜVENLİK PLANI GELMEDİ",
            YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi => "TEMEL TOPRAKLAMA TUTANAĞI HAZIRLANMADI",
            _ => BuildFallbackSummaryReason(columnKey)
        };

    private static int GetYibfIsTakibiColumnOrder(string? columnKey)
        => columnKey switch
        {
            YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi => 0,
            YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi => 1,
            YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi => 2,
            YibfIsTakibiColumnKeys.EvraklarTamMi => 3,
            YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi => 4,
            YibfIsTakibiColumnKeys.DekontAlindiMi => 5,
            YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi => 6,
            YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi => 7,
            YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi => 8,
            YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi => 9,
            YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi => 10,
            YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi => 11,
            _ => int.MaxValue
        };

    private static int GetTadilatColumnOrder(string? columnKey)
        => columnKey switch
        {
            TadilatColumnKeys.DigitalReceived => 0,
            TadilatColumnKeys.InspectorApproved => 1,
            TadilatColumnKeys.OutputAndReportArrived => 2,
            TadilatColumnKeys.OfficialLetterSubmitted => 3,
            TadilatColumnKeys.ArchivedFromMunicipality => 4,
            _ => int.MaxValue
        };

    private static bool IsPendingSummaryColor(string? color)
        => IsRedSummaryColor(color) || IsYellowSummaryColor(color);

    private static bool IsRedSummaryColor(string? color)
        => string.Equals(color, StrongRedColor, StringComparison.OrdinalIgnoreCase)
           || string.Equals(color, LegacyPaleRedColor, StringComparison.OrdinalIgnoreCase);

    private static bool IsYellowSummaryColor(string? color)
        => string.Equals(color, StrongYellowColor, StringComparison.OrdinalIgnoreCase)
           || string.Equals(color, LegacyPaleYellowColor, StringComparison.OrdinalIgnoreCase);

    private static string BuildFallbackSummaryReason(string? columnKey)
    {
        if (string.IsNullOrWhiteSpace(columnKey))
        {
            return string.Empty;
        }

        return string.Concat(
            columnKey
                .Replace('_', ' ')
                .Select((ch, index) => index > 0 && char.IsUpper(ch) ? $" {ch}" : ch.ToString()))
            .Trim()
            .ToUpperInvariant();
    }
}
