using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ProjectCatalogService : IProjectCatalogService
{
    private readonly IProjectCatalogRepository _repository;

    public ProjectCatalogService(IProjectCatalogRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ProjectCatalogEntry>> LoadAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task SaveAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
        => _repository.SaveManyAsync(entries, cancellationToken);

    public IReadOnlyList<ProjectCatalogEntry> Search(IEnumerable<ProjectCatalogEntry> source, string? query)
    {
        var catalog = source as IReadOnlyList<ProjectCatalogEntry> ?? source.ToList();
        var normalizedQuery = SearchTextNormalizer.Normalize(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return catalog.OrderBy(item => item.DisplayOrder).ThenBy(item => item.DisplayName).ToList();
        }

        return catalog
            .Where(item => ProjectCatalogIdentityHelper.MatchesSearch(item, query, catalog))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.DisplayName)
            .ToList();
    }

    public IReadOnlyList<ProjectCatalogEntry> BuildSeedFromAnaBilgi(IEnumerable<YibfAnaBilgiEntry> anaBilgi)
    {
        var seen = new HashSet<Guid>();
        var results = new List<ProjectCatalogEntry>();
        var order = 0;

        foreach (var entry in anaBilgi.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt))
        {
            var catalogId = entry.WorkGroupId != Guid.Empty ? entry.WorkGroupId : entry.Id;
            if (catalogId == Guid.Empty || !seen.Add(catalogId))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.AdaParsel) && string.IsNullOrWhiteSpace(entry.YapiSahibi))
            {
                continue;
            }

            results.Add(new ProjectCatalogEntry
            {
                Id = catalogId,
                DisplayName = BuildDisplayName(entry.AdaParsel ?? string.Empty, entry.YapiSahibi ?? string.Empty),
                AdaParsel = entry.AdaParsel ?? string.Empty,
                YapiSahibi = entry.YapiSahibi ?? string.Empty,
                YibfNo = entry.YibfNo ?? string.Empty,
                Belediye = entry.Idare ?? string.Empty,
                Muteahhit = entry.Muteahhit ?? string.Empty,
                Kind = ProjectCatalogKind.Normal,
                IsActive = true,
                DisplayOrder = order++,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        return results;
    }

    public ProjectCatalogFanOutResult BuildFanOut(ProjectCatalogEntry entry)
    {
        return entry.Kind switch
        {
            ProjectCatalogKind.Normal => new ProjectCatalogFanOutResult
            {
                AnaBilgiStub = BuildAnaBilgiStub(entry),
                IsTakibiStub = BuildIsTakibiStub(entry, parentProjectId: null)
            },
            ProjectCatalogKind.Istinat when entry.ParentProjectId is Guid parentId && parentId != Guid.Empty => new ProjectCatalogFanOutResult
            {
                IsTakibiStub = BuildIsTakibiStub(entry, parentProjectId: parentId)
            },
            ProjectCatalogKind.Istinat => throw new InvalidOperationException("İstinat projeleri için üst proje seçilmelidir."),
            _ => new ProjectCatalogFanOutResult()
        };
    }

    public void ApplyProjectSelection(
        KarotEntry entry,
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        entry.ProjectId = project.Id;
        var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(project, catalog);
        FillIfEmpty(entry.AdaParsel, identity.AdaParsel, value => entry.AdaParsel = value);
        FillIfEmpty(entry.YapiSahibi, identity.YapiSahibi, value => entry.YapiSahibi = value);
        FillIfEmpty(entry.YibfNo, identity.YibfNo, value => entry.YibfNo = value);
        FillIfEmpty(entry.Muteahhit, identity.Muteahhit, value => entry.Muteahhit = value);
    }

    public void ApplyProjectSelection(
        TadilatEntry entry,
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        entry.ProjectId = project.Id;
        FillIfEmpty(entry.JobName, BuildEffectiveJobName(project, catalog), value => entry.JobName = value);
    }

    public void ApplyProjectSelection(
        ActionEntry entry,
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        entry.ProjectId = project.Id;
        var ownerParcel = ProjectCatalogIdentityHelper.BuildEffectiveOwnerParcelText(project, catalog);
        FillIfEmpty(entry.OwnerParcelText, ownerParcel, value => entry.OwnerParcelText = value);
        // WorkText bilinçli olarak doldurulmaz; yapılacak iş kullanıcının yazdığı metindir.
    }

    public void ApplyProjectSelection(
        MissingProjectEntry entry,
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        entry.ProjectId = project.Id;
        var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(project, catalog);
        FillIfEmpty(entry.AdaParsel, identity.AdaParsel, value => entry.AdaParsel = value);
        FillIfEmpty(entry.YapiSahibi, identity.YapiSahibi, value => entry.YapiSahibi = value);
    }

    public void ApplyProjectSelection(TaskItem entry, ProjectCatalogEntry project)
    {
        entry.ProjectId = project.Id;
        entry.IsSpecialJob = project.Kind == ProjectCatalogKind.Special;
        FillIfEmpty(entry.Title, project.DisplayName, value => entry.Title = value);
    }

    public void ApplyProjectSelection(
        YibfIsTakibiEntry entry,
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        if (project.Kind == ProjectCatalogKind.Istinat)
        {
            if (project.ParentProjectId is not Guid parentId || parentId == Guid.Empty)
            {
                throw new InvalidOperationException("İstinat projeleri için üst proje seçilmelidir.");
            }

            entry.WorkGroupId = parentId;
            entry.WorkIdentityId = project.Id;
        }
        else
        {
            entry.WorkGroupId = project.Id;
            entry.WorkIdentityId = project.Id;
        }

        FillIfEmpty(entry.JobName, BuildEffectiveJobName(project, catalog), value => entry.JobName = value);
    }

    public void ApplyProjectSelection(YibfAnaBilgiEntry entry, ProjectCatalogEntry project)
    {
        FillIfEmpty(entry.AdaParsel, project.AdaParsel, value => entry.AdaParsel = value);
        FillIfEmpty(entry.YapiSahibi, project.YapiSahibi, value => entry.YapiSahibi = value);
        FillIfEmpty(entry.YibfNo, project.YibfNo, value => entry.YibfNo = value);
        FillIfEmpty(entry.Idare, project.Belediye, value => entry.Idare = value);
        FillIfEmpty(entry.Muteahhit, project.Muteahhit, value => entry.Muteahhit = value);
    }

    public ProjectCatalogSyncResult PreviewLinkedIdentityOverwrite(
        ProjectCatalogEntry project,
        IReadOnlyList<KarotEntry> karot,
        IReadOnlyList<MissingProjectEntry> missing,
        IReadOnlyList<ActionEntry> action,
        IReadOnlyList<TadilatEntry> tadilat,
        IReadOnlyList<YibfIsTakibiEntry> yibfIsTakibi)
        => SynchronizeLinkedIdentityFields(project, karot, missing, action, tadilat, yibfIsTakibi, apply: false);

    public ProjectCatalogSyncResult OverwriteLinkedIdentityFields(
        ProjectCatalogEntry project,
        IReadOnlyList<KarotEntry> karot,
        IReadOnlyList<MissingProjectEntry> missing,
        IReadOnlyList<ActionEntry> action,
        IReadOnlyList<TadilatEntry> tadilat,
        IReadOnlyList<YibfIsTakibiEntry> yibfIsTakibi)
        => SynchronizeLinkedIdentityFields(project, karot, missing, action, tadilat, yibfIsTakibi, apply: true);

    private static ProjectCatalogSyncResult SynchronizeLinkedIdentityFields(
        ProjectCatalogEntry project,
        IReadOnlyList<KarotEntry> karot,
        IReadOnlyList<MissingProjectEntry> missing,
        IReadOnlyList<ActionEntry> action,
        IReadOnlyList<TadilatEntry> tadilat,
        IReadOnlyList<YibfIsTakibiEntry> yibfIsTakibi,
        bool apply)
    {
        var adaParsel = project.AdaParsel?.Trim() ?? string.Empty;
        var yapiSahibi = project.YapiSahibi?.Trim() ?? string.Empty;
        var yibfNo = project.YibfNo?.Trim() ?? string.Empty;
        var muteahhit = project.Muteahhit?.Trim() ?? string.Empty;
        var ownerParcel = BuildOwnerParcelText(project);
        var jobName = ChooseRicherText(project.DisplayName, ownerParcel);
        var now = DateTime.Now;

        var karotCount = 0;
        foreach (var entry in karot.Where(item => item.ProjectId == project.Id))
        {
            if (entry.AdaParsel == adaParsel
                && entry.YapiSahibi == yapiSahibi
                && entry.YibfNo == yibfNo
                && entry.Muteahhit == muteahhit)
            {
                continue;
            }

            karotCount++;
            if (apply)
            {
                entry.AdaParsel = adaParsel;
                entry.YapiSahibi = yapiSahibi;
                entry.YibfNo = yibfNo;
                entry.Muteahhit = muteahhit;
                entry.UpdatedAt = now;
            }
        }

        var missingCount = 0;
        foreach (var entry in missing.Where(item => item.ProjectId == project.Id))
        {
            if (entry.AdaParsel == adaParsel && entry.YapiSahibi == yapiSahibi)
            {
                continue;
            }

            missingCount++;
            if (apply)
            {
                entry.AdaParsel = adaParsel;
                entry.YapiSahibi = yapiSahibi;
                entry.UpdatedAt = now;
            }
        }

        var actionCount = 0;
        foreach (var entry in action.Where(item => item.ProjectId == project.Id))
        {
            if (entry.OwnerParcelText == ownerParcel)
            {
                continue;
            }

            actionCount++;
            if (apply)
            {
                entry.OwnerParcelText = ownerParcel;
                entry.UpdatedAt = now;
            }
        }

        var tadilatCount = 0;
        foreach (var entry in tadilat.Where(item => item.ProjectId == project.Id))
        {
            if (entry.JobName == jobName)
            {
                continue;
            }

            tadilatCount++;
            if (apply)
            {
                entry.JobName = jobName;
                entry.UpdatedAt = now;
            }
        }

        var yibfCount = 0;
        foreach (var entry in yibfIsTakibi.Where(item =>
                     item.WorkIdentityId == project.Id
                     || (project.Kind != ProjectCatalogKind.Istinat && item.WorkGroupId == project.Id)))
        {
            if (entry.JobName == jobName)
            {
                continue;
            }

            yibfCount++;
            if (apply)
            {
                entry.JobName = jobName;
                entry.UpdatedAt = now;
            }
        }

        return new ProjectCatalogSyncResult
        {
            KarotCount = karotCount,
            MissingProjectCount = missingCount,
            ActionCount = actionCount,
            TadilatCount = tadilatCount,
            YibfIsTakibiCount = yibfCount
        };
    }

    private static YibfAnaBilgiEntry BuildAnaBilgiStub(ProjectCatalogEntry entry)
    {
        var now = DateTime.Now;
        return new YibfAnaBilgiEntry
        {
            Id = entry.Id,
            WorkGroupId = entry.Id,
            WorkIdentityId = entry.Id,
            AdaParsel = entry.AdaParsel ?? string.Empty,
            YapiSahibi = entry.YapiSahibi ?? string.Empty,
            YibfNo = entry.YibfNo ?? string.Empty,
            Idare = entry.Belediye ?? string.Empty,
            Muteahhit = entry.Muteahhit ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static YibfIsTakibiEntry BuildIsTakibiStub(ProjectCatalogEntry entry, Guid? parentProjectId)
    {
        var groupId = entry.Kind == ProjectCatalogKind.Istinat && parentProjectId is Guid parent && parent != Guid.Empty
            ? parent
            : entry.Id;
        var now = DateTime.Now;
        return new YibfIsTakibiEntry
        {
            WorkGroupId = groupId,
            WorkIdentityId = entry.Id,
            WorkVariantLabel = entry.Kind == ProjectCatalogKind.Istinat
                ? (string.IsNullOrWhiteSpace(entry.DisplayName) || string.Equals(entry.DisplayName.Trim(), "Istinat", StringComparison.OrdinalIgnoreCase)
                    ? "İstinat"
                    : entry.DisplayName.Trim())
                : string.Empty,
            JobName = entry.DisplayName ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string BuildDisplayName(string adaParsel, string yapiSahibi)
    {
        var parts = new[] { adaParsel?.Trim(), yapiSahibi?.Trim() }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(' ', parts);
    }

    private static string BuildOwnerParcelText(ProjectCatalogEntry project)
        => ProjectCatalogIdentityHelper.BuildEffectiveOwnerParcelText(project);

    private static string BuildEffectiveJobName(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog)
    {
        if (project.Kind == ProjectCatalogKind.Istinat)
        {
            return ProjectCatalogIdentityHelper.BuildPickerTitle(project, catalog);
        }

        var ownerParcel = ProjectCatalogIdentityHelper.BuildEffectiveOwnerParcelText(project, catalog);
        return ChooseRicherText(project.DisplayName, ownerParcel);
    }

    private static string ChooseRicherText(string? primary, string? secondary)
    {
        var left = primary?.Trim() ?? string.Empty;
        var right = secondary?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(left))
        {
            return right;
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            return left;
        }

        return right.Length > left.Length ? right : left;
    }

    private static void FillIfEmpty(string current, string? candidate, Action<string> assign)
    {
        if (!string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        assign(candidate.Trim());
    }
}
