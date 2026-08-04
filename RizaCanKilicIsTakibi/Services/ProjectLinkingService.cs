using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Text.RegularExpressions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ProjectLinkingService : IProjectLinkingService
{
    private static readonly Regex AdaParselPattern = new(@"\d+\s*[-/]\s*\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IProjectCatalogService _catalogService;

    public ProjectLinkingService(IProjectCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public ProjectLinkDryRunResult DryRun(
        IReadOnlyList<ProjectCatalogEntry> catalog,
        IReadOnlyList<KarotEntry> karot,
        IReadOnlyList<TadilatEntry> tadilat,
        IReadOnlyList<ActionEntry> action,
        IReadOnlyList<MissingProjectEntry> missing,
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyList<YibfIsTakibiEntry> yibfIsTakibi)
    {
        var autoActions = new List<AutoProjectLinkAction>();
        var unresolved = new List<UnresolvedProjectLinkItem>();
        var skipped = 0;
        var specialCount = 0;

        foreach (var entry in karot)
        {
            if (entry.Status == KarotStatus.KarotAlindiOlumlu)
            {
                skipped++;
                continue;
            }

            ProcessEntry(
                ProjectLinkSourceModule.Karot,
                entry.Id,
                BuildSummary(entry.AdaParsel, entry.YapiSahibi, entry.YibfNo),
                entry.AdaParsel,
                entry.YapiSahibi,
                entry.YibfNo,
                entry.ProjectId,
                isSpecial: false,
                preferIstinat: ContainsIstinat(entry.Aciklama, entry.KatBilgisi),
                catalog,
                autoActions,
                unresolved,
                ref skipped,
                ref specialCount);
        }

        foreach (var entry in tadilat)
        {
            if (entry.SubTab == TadilatSubTab.Biten)
            {
                skipped++;
                continue;
            }

            ProcessEntry(
                ProjectLinkSourceModule.Tadilat,
                entry.Id,
                BuildSummary(entry.JobName, entry.ProjectType, entry.District),
                TryExtractAdaParsel(entry.JobName) ?? string.Empty,
                TryExtractYapiSahibi(entry.JobName) ?? string.Empty,
                yibfNo: string.Empty,
                entry.ProjectId,
                isSpecial: false,
                preferIstinat: ContainsIstinat(entry.JobName, entry.ProjectType, entry.Description1, entry.Description2),
                catalog,
                autoActions,
                unresolved,
                ref skipped,
                ref specialCount);
        }

        foreach (var entry in action)
        {
            ProcessEntry(
                ProjectLinkSourceModule.Aksiyon,
                entry.Id,
                BuildSummary(entry.OwnerParcelText, entry.WorkText, entry.District),
                TryExtractAdaParsel(entry.OwnerParcelText) ?? string.Empty,
                TryExtractYapiSahibi(entry.OwnerParcelText) ?? string.Empty,
                yibfNo: string.Empty,
                entry.ProjectId,
                isSpecial: false,
                preferIstinat: ContainsIstinat(entry.WorkText, entry.OwnerParcelText),
                catalog,
                autoActions,
                unresolved,
                ref skipped,
                ref specialCount);
        }

        foreach (var entry in missing)
        {
            ProcessEntry(
                ProjectLinkSourceModule.EksikProje,
                entry.Id,
                BuildSummary(entry.AdaParsel, entry.YapiSahibi, entry.MissingProjectText),
                entry.AdaParsel,
                entry.YapiSahibi,
                yibfNo: string.Empty,
                entry.ProjectId,
                isSpecial: false,
                preferIstinat: ContainsIstinat(entry.MissingProjectText, entry.Description),
                catalog,
                autoActions,
                unresolved,
                ref skipped,
                ref specialCount);
        }

        foreach (var entry in tasks.Where(task => task.BoardType == TaskBoardType.Genel))
        {
            var adaParsel = TryExtractAdaParsel(entry.Title) ?? TryExtractAdaParsel(entry.Description);
            var yapiSahibi = TryExtractYapiSahibi(entry.Title) ?? TryExtractYapiSahibi(entry.Description);
            var hasSignals = HasProjectSignals(adaParsel ?? string.Empty, yapiSahibi ?? string.Empty, string.Empty, entry.Title, entry.Description);

            if (!hasSignals)
            {
                // Proje sinyali yoksa şüpheli listesine alma; atla.
                skipped++;
                continue;
            }

            ProcessEntry(
                ProjectLinkSourceModule.GenelIs,
                entry.Id,
                BuildSummary(entry.Title, entry.Description),
                adaParsel ?? string.Empty,
                yapiSahibi ?? string.Empty,
                yibfNo: string.Empty,
                entry.ProjectId,
                entry.IsSpecialJob,
                preferIstinat: ContainsIstinat(entry.Title, entry.Description),
                catalog,
                autoActions,
                unresolved,
                ref skipped,
                ref specialCount);
        }

        foreach (var entry in yibfIsTakibi)
        {
            if (IsYibfIsTakibiLinked(entry, catalog))
            {
                skipped++;
                continue;
            }

            var adaParsel = TryExtractAdaParsel(entry.JobName);
            var yapiSahibi = TryExtractYapiSahibi(entry.JobName);
            ProcessEntry(
                ProjectLinkSourceModule.YibfIsTakibi,
                entry.Id,
                BuildSummary(entry.JobName, entry.WorkVariantLabel),
                adaParsel ?? string.Empty,
                yapiSahibi ?? string.Empty,
                yibfNo: string.Empty,
                projectId: null,
                isSpecial: false,
                preferIstinat: ContainsIstinat(entry.JobName, entry.WorkVariantLabel),
                catalog,
                autoActions,
                unresolved,
                ref skipped,
                ref specialCount);
        }

        return new ProjectLinkDryRunResult
        {
            AutoLinkCount = autoActions.Count(action => !action.MarkSpecialJob),
            SpecialJobCount = autoActions.Count(action => action.MarkSpecialJob),
            SkippedAlreadyLinkedCount = skipped,
            Unresolved = unresolved,
            AutoActions = autoActions
        };
    }

    public void Apply(
        IReadOnlyList<AutoProjectLinkAction> autoActions,
        IReadOnlyList<UnresolvedLinkResolution> userResolutions,
        IList<KarotEntry> karot,
        IList<TadilatEntry> tadilat,
        IList<ActionEntry> action,
        IList<MissingProjectEntry> missing,
        IList<TaskItem> tasks,
        IList<YibfIsTakibiEntry> yibfIsTakibi,
        IList<ProjectCatalogEntry> catalog)
    {
        foreach (var linkAction in autoActions)
        {
            ApplyAction(linkAction, karot, tadilat, action, missing, tasks, yibfIsTakibi, catalog, newCatalog: null);
        }

        foreach (var resolution in userResolutions)
        {
            switch (resolution.Kind)
            {
                case UnresolvedLinkResolutionKind.Skip:
                    continue;
                case UnresolvedLinkResolutionKind.MarkSpecialJob:
                    ApplyAction(new AutoProjectLinkAction
                    {
                        Module = resolution.Module,
                        EntryId = resolution.EntryId,
                        MarkSpecialJob = true
                    }, karot, tadilat, action, missing, tasks, yibfIsTakibi, catalog, newCatalog: null);
                    break;
                case UnresolvedLinkResolutionKind.LinkToProject when resolution.ProjectId is Guid projectId:
                    ApplyAction(new AutoProjectLinkAction
                    {
                        Module = resolution.Module,
                        EntryId = resolution.EntryId,
                        ProjectId = projectId
                    }, karot, tadilat, action, missing, tasks, yibfIsTakibi, catalog, newCatalog: null);
                    break;
                case UnresolvedLinkResolutionKind.CreateCatalogAndLink when resolution.NewCatalogEntry is not null:
                    ApplyAction(new AutoProjectLinkAction
                    {
                        Module = resolution.Module,
                        EntryId = resolution.EntryId,
                        ProjectId = resolution.NewCatalogEntry.Id
                    }, karot, tadilat, action, missing, tasks, yibfIsTakibi, catalog, resolution.NewCatalogEntry);
                    break;
            }
        }
    }

    private void ApplyAction(
        AutoProjectLinkAction linkAction,
        IList<KarotEntry> karot,
        IList<TadilatEntry> tadilat,
        IList<ActionEntry> action,
        IList<MissingProjectEntry> missing,
        IList<TaskItem> tasks,
        IList<YibfIsTakibiEntry> yibfIsTakibi,
        IList<ProjectCatalogEntry> catalog,
        ProjectCatalogEntry? newCatalog)
    {
        if (newCatalog is not null && !catalog.Any(item => item.Id == newCatalog.Id))
        {
            catalog.Add(newCatalog);
        }

        if (linkAction.MarkSpecialJob)
        {
            if (linkAction.Module == ProjectLinkSourceModule.GenelIs)
            {
                var task = tasks.FirstOrDefault(item => item.Id == linkAction.EntryId);
                if (task is not null)
                {
                    task.IsSpecialJob = true;
                    task.ProjectId = null;
                }
            }

            return;
        }

        if (linkAction.ProjectId is not Guid projectId)
        {
            return;
        }

        var project = catalog.FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return;
        }

        switch (linkAction.Module)
        {
            case ProjectLinkSourceModule.Karot:
                if (karot.FirstOrDefault(item => item.Id == linkAction.EntryId) is { } karotEntry)
                {
                    _catalogService.ApplyProjectSelection(karotEntry, project, catalog);
                }
                break;
            case ProjectLinkSourceModule.Tadilat:
                if (tadilat.FirstOrDefault(item => item.Id == linkAction.EntryId) is { } tadilatEntry)
                {
                    _catalogService.ApplyProjectSelection(tadilatEntry, project);
                }
                break;
            case ProjectLinkSourceModule.Aksiyon:
                if (action.FirstOrDefault(item => item.Id == linkAction.EntryId) is { } actionEntry)
                {
                    _catalogService.ApplyProjectSelection(actionEntry, project);
                }
                break;
            case ProjectLinkSourceModule.EksikProje:
                if (missing.FirstOrDefault(item => item.Id == linkAction.EntryId) is { } missingEntry)
                {
                    _catalogService.ApplyProjectSelection(missingEntry, project);
                }
                break;
            case ProjectLinkSourceModule.GenelIs:
                if (tasks.FirstOrDefault(item => item.Id == linkAction.EntryId) is { } taskEntry)
                {
                    _catalogService.ApplyProjectSelection(taskEntry, project);
                }
                break;
            case ProjectLinkSourceModule.YibfIsTakibi:
                if (yibfIsTakibi.FirstOrDefault(item => item.Id == linkAction.EntryId) is { } isTakibiEntry)
                {
                    _catalogService.ApplyProjectSelection(isTakibiEntry, project);
                }
                break;
        }
    }

    private static void ProcessEntry(
        ProjectLinkSourceModule module,
        Guid entryId,
        string summaryText,
        string adaParsel,
        string yapiSahibi,
        string yibfNo,
        Guid? projectId,
        bool isSpecial,
        bool preferIstinat,
        IReadOnlyList<ProjectCatalogEntry> catalog,
        IList<AutoProjectLinkAction> autoActions,
        IList<UnresolvedProjectLinkItem> unresolved,
        ref int skipped,
        ref int specialCount)
    {
        if (projectId is not null || isSpecial)
        {
            skipped++;
            return;
        }

        if (!HasProjectSignals(adaParsel, yapiSahibi, yibfNo, summaryText))
        {
            // Sinyal yok → şüpheli değil, atla.
            skipped++;
            return;
        }

        var candidates = ScoreCandidates(catalog, adaParsel, yapiSahibi, yibfNo, summaryText, preferIstinat);
        if (candidates.Count == 0)
        {
            // Katalogda benzer aday yok → şüpheli değil, atla.
            skipped++;
            return;
        }

        var autoProjectId = TryPickAutoLinkProject(candidates, preferIstinat);
        if (autoProjectId is Guid linkedProjectId)
        {
            autoActions.Add(new AutoProjectLinkAction
            {
                Module = module,
                EntryId = entryId,
                ProjectId = linkedProjectId
            });
            return;
        }

        // Aday var ama tek net seçim yok → şüpheli.
        unresolved.Add(new UnresolvedProjectLinkItem
        {
            Module = module,
            EntryId = entryId,
            SummaryText = summaryText,
            AdaParsel = adaParsel ?? string.Empty,
            YapiSahibi = yapiSahibi ?? string.Empty,
            Candidates = candidates
        });
    }

    private static Guid? TryPickAutoLinkProject(
        IReadOnlyList<ProjectLinkCandidate> candidates,
        bool preferIstinat)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var yibfMatches = candidates.Where(candidate => candidate.HasYibfMatch).ToList();
        var pickedYibf = PickPreferredCandidate(yibfMatches, preferIstinat, allowFamilyTieBreak: true);
        if (pickedYibf is not null)
        {
            return pickedYibf.ProjectId;
        }

        // En emin: ada/parsel tam + yapı sahibi ilk kelimeleri tam (sıralı).
        var firstWordsMatches = candidates
            .Where(candidate => candidate.HasAdaMatch && candidate.HasOwnerFirstWordsMatch)
            .ToList();
        var pickedFirstWords = PickPreferredCandidate(firstWordsMatches, preferIstinat, allowFamilyTieBreak: true);
        if (pickedFirstWords is not null)
        {
            return pickedFirstWords.ProjectId;
        }

        // Ada + sahip benzerliği (eski kural).
        var identityMatches = candidates
            .Where(candidate => candidate.HasAdaMatch && candidate.HasOwnerMatch)
            .ToList();
        var pickedIdentity = PickPreferredCandidate(identityMatches, preferIstinat, allowFamilyTieBreak: true);
        if (pickedIdentity is not null)
        {
            return pickedIdentity.ProjectId;
        }

        // Belirsiz skorlu / zayıf adayları otomatik bağlama — kullanıcıya sorulsun.
        return null;
    }

    private static ProjectLinkCandidate? PickPreferredCandidate(
        IReadOnlyList<ProjectLinkCandidate> candidates,
        bool preferIstinat,
        bool allowFamilyTieBreak)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var topScore = candidates.Max(candidate => candidate.Score);
        var top = candidates.Where(candidate => candidate.Score == topScore).ToList();
        if (top.Count == 1)
        {
            return top[0];
        }

        if (!allowFamilyTieBreak)
        {
            return null;
        }

        // Aynı kimlik ailesinde Normal/İstinat çakışmasını çöz; farklı projeler belirsiz kalsın.
        var sameIdentityFamily = top.All(candidate =>
            (candidate.HasAdaMatch && (candidate.HasOwnerFirstWordsMatch || candidate.HasOwnerMatch))
            || candidate.HasYibfMatch);
        if (!sameIdentityFamily)
        {
            return null;
        }

        if (preferIstinat)
        {
            var istinat = top.Where(candidate => candidate.Kind == ProjectCatalogKind.Istinat).ToList();
            if (istinat.Count == 1)
            {
                return istinat[0];
            }

            var normalFallback = top.Where(candidate => candidate.Kind == ProjectCatalogKind.Normal).ToList();
            return normalFallback.Count == 1 ? normalFallback[0] : null;
        }

        var normal = top.Where(candidate => candidate.Kind == ProjectCatalogKind.Normal).ToList();
        if (normal.Count == 1)
        {
            return normal[0];
        }

        var istinatFallback = top.Where(candidate => candidate.Kind == ProjectCatalogKind.Istinat).ToList();
        return istinatFallback.Count == 1 ? istinatFallback[0] : null;
    }

    private static IReadOnlyList<ProjectLinkCandidate> ScoreCandidates(
        IReadOnlyList<ProjectCatalogEntry> catalog,
        string adaParsel,
        string yapiSahibi,
        string yibfNo,
        string summaryText,
        bool preferIstinat)
    {
        var results = new List<ProjectLinkCandidate>();
        var sourceAda = NormalizeAdaParsel(adaParsel);
        if (string.IsNullOrWhiteSpace(sourceAda))
        {
            sourceAda = NormalizeAdaParsel(TryExtractAdaParsel(summaryText));
        }

        var sourceOwner = NormalizeOwnerName(yapiSahibi);
        if (string.IsNullOrWhiteSpace(sourceOwner))
        {
            sourceOwner = NormalizeOwnerName(TryExtractYapiSahibi(summaryText));
        }

        var sourceSummary = SearchTextNormalizer.Normalize(summaryText);

        foreach (var project in catalog.Where(item => item.IsActive))
        {
            var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(project, catalog);
            var projectAda = NormalizeAdaParsel(identity.AdaParsel);
            if (string.IsNullOrWhiteSpace(projectAda))
            {
                projectAda = NormalizeAdaParsel(project.DisplayName);
            }

            var projectOwner = NormalizeOwnerName(identity.YapiSahibi);
            var projectDisplayOwner = NormalizeOwnerName(TryExtractYapiSahibi(project.DisplayName) ?? project.DisplayName);
            var projectDisplay = SearchTextNormalizer.Normalize(project.DisplayName);

            var hasAdaMatch = !string.IsNullOrWhiteSpace(sourceAda)
                              && !string.IsNullOrWhiteSpace(projectAda)
                              && string.Equals(sourceAda, projectAda, StringComparison.Ordinal);

            var hasOwnerFirstWordsMatch = HasOwnerFirstWordsMatch(sourceOwner, projectOwner)
                                          || HasOwnerFirstWordsMatch(sourceOwner, projectDisplayOwner);
            var hasOwnerMatch = hasOwnerFirstWordsMatch || HasOwnerMatch(sourceOwner, projectOwner) || HasOwnerMatch(sourceOwner, projectDisplayOwner);
            var hasDisplayMatch = !string.IsNullOrWhiteSpace(sourceSummary)
                                  && !string.IsNullOrWhiteSpace(projectDisplay)
                                  && (sourceSummary == projectDisplay
                                      || sourceSummary.Contains(projectDisplay, StringComparison.Ordinal)
                                      || projectDisplay.Contains(sourceSummary, StringComparison.Ordinal));

            var score = 0;
            if (hasAdaMatch)
            {
                score += 40;
            }

            if (hasOwnerFirstWordsMatch)
            {
                score += EqualsOwnerExact(sourceOwner, projectOwner) ? 45 : 35;
            }
            else if (hasOwnerMatch)
            {
                score += 20;
            }

            var hasYibfMatch = !string.IsNullOrWhiteSpace(yibfNo)
                              && SearchTextNormalizer.EqualsNormalized(identity.YibfNo, yibfNo);
            if (hasYibfMatch)
            {
                score += 50;
            }

            if (hasDisplayMatch && score < 40)
            {
                score += 15;
            }

            if (preferIstinat)
            {
                if (project.Kind == ProjectCatalogKind.Istinat)
                {
                    score += 15;
                }
                else if (project.Kind == ProjectCatalogKind.Normal)
                {
                    score -= 5;
                }
            }

            if (score > 0)
            {
                results.Add(new ProjectLinkCandidate
                {
                    ProjectId = project.Id,
                    DisplayName = project.DisplayName,
                    Kind = project.Kind,
                    Score = score,
                    HasAdaMatch = hasAdaMatch,
                    HasOwnerMatch = hasOwnerMatch,
                    HasOwnerFirstWordsMatch = hasOwnerFirstWordsMatch,
                    HasYibfMatch = hasYibfMatch
                });
            }
        }

        return results
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Kind == ProjectCatalogKind.Normal ? 0 : 1)
            .ThenBy(candidate => candidate.DisplayName)
            .ToList();
    }

    private static string NormalizeAdaParsel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var match = AdaParselPattern.Match(value);
        var raw = match.Success ? match.Value : value;
        var normalized = SearchTextNormalizer.Normalize(raw);
        var builder = new System.Text.StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.IsDigit(ch))
            {
                builder.Append(ch);
            }
            else if (ch is '-' or '/' or '\\')
            {
                builder.Append('-');
            }
        }

        return builder.ToString();
    }

    private static string NormalizeOwnerName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Ada/parsel kalıntısını temizle.
        var remainder = value.Trim();
        var match = AdaParselPattern.Match(remainder);
        if (match.Success)
        {
            remainder = remainder.Remove(match.Index, match.Length).Trim();
        }

        return SearchTextNormalizer.Normalize(remainder);
    }

    private static bool EqualsOwnerExact(string left, string right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left, right, StringComparison.Ordinal);

    private static bool HasOwnerFirstWordsMatch(string left, string right)
    {
        if (EqualsOwnerExact(left, right))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var leftTokens = TokenizeOwner(left);
        var rightTokens = TokenizeOwner(right);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return false;
        }

        // En az 2 kelime varsa ilk 2 kelime sırayla aynı olmalı; tek kelimede en az 4 harf.
        var take = Math.Min(leftTokens.Count, rightTokens.Count) >= 2 ? 2 : 1;
        if (take == 1 && Math.Min(leftTokens[0].Length, rightTokens[0].Length) < 4)
        {
            return false;
        }

        for (var index = 0; index < take; index++)
        {
            if (!string.Equals(leftTokens[index], rightTokens[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasOwnerMatch(string left, string right)
    {
        if (HasOwnerFirstWordsMatch(left, right))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var leftTokens = TokenizeOwner(left);
        var rightTokens = TokenizeOwner(right);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return false;
        }

        // Kısa olanın tüm anlamlı tokenleri uzun olanda varsa benzer kabul et.
        var shorter = leftTokens.Count <= rightTokens.Count ? leftTokens : rightTokens;
        var longer = leftTokens.Count <= rightTokens.Count ? rightTokens : leftTokens;
        if (shorter.Count == 0)
        {
            return false;
        }

        var matched = shorter.Count(token => longer.Contains(token));
        if (matched == shorter.Count && shorter.Count >= 2)
        {
            return true;
        }

        // Tek token ve yeterince uzunsa (soyad vb.)
        return shorter.Count == 1 && shorter[0].Length >= 5 && longer.Contains(shorter[0]);
    }

    private static List<string> TokenizeOwner(string normalizedOwner)
        => normalizedOwner
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2)
            .Where(token => token is not ("insaat" or "ltd" or "sti" or "as" or "san" or "tic" or "sirketi" or "belediyesi" or "belediye"))
            .ToList();

    private static bool HasProjectSignals(string adaParsel, string yapiSahibi, string yibfNo, params string?[] extraText)
    {
        if (!string.IsNullOrWhiteSpace(adaParsel)
            || !string.IsNullOrWhiteSpace(yapiSahibi)
            || !string.IsNullOrWhiteSpace(yibfNo))
        {
            return true;
        }

        return extraText.Any(text => HasAdaParselSignal(text));
    }

    private static bool HasAdaParselSignal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (AdaParselPattern.IsMatch(text))
        {
            return true;
        }

        var normalized = SearchTextNormalizer.Normalize(text);
        return normalized.Contains("ada", StringComparison.Ordinal)
               || normalized.Contains("parsel", StringComparison.Ordinal);
    }

    private static bool ContainsIstinat(params string?[] values)
        => values.Any(value => SearchTextNormalizer.Normalize(value).Contains("istinat", StringComparison.Ordinal));

    private static bool IsYibfIsTakibiLinked(YibfIsTakibiEntry entry, IReadOnlyList<ProjectCatalogEntry> catalog)
    {
        if (catalog.Any(project => project.Id == entry.WorkIdentityId))
        {
            return true;
        }

        // Normal/özel proje grubuna bağlanmış varyant satırları (WorkIdentityId satırın kendi Id'si olabilir).
        return catalog.Any(project =>
            project.Id == entry.WorkGroupId
            && project.Kind != ProjectCatalogKind.Istinat);
    }

    private static string? TryExtractAdaParsel(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = AdaParselPattern.Match(text);
        return match.Success ? match.Value.Replace(" ", string.Empty) : null;
    }

    private static string? TryExtractYapiSahibi(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var remainder = text.Trim();
        var match = AdaParselPattern.Match(remainder);
        if (match.Success)
        {
            remainder = remainder.Remove(match.Index, match.Length).Trim();
        }

        return string.IsNullOrWhiteSpace(remainder) ? null : remainder;
    }

    private static string BuildSummary(params string?[] parts)
        => string.Join(" | ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

    private static void FillIfEmpty(string current, string candidate, Action<string> assign)
    {
        if (!string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        assign(candidate.Trim());
    }
}
