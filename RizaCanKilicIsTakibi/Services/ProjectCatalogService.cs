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
        var normalizedQuery = SearchTextNormalizer.Normalize(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return source.OrderBy(item => item.DisplayOrder).ThenBy(item => item.DisplayName).ToList();
        }

        return source
            .Where(item => SearchTextNormalizer.Contains(item.DisplayName, query)
                           || SearchTextNormalizer.Contains(item.AdaParsel, query)
                           || SearchTextNormalizer.Contains(item.YapiSahibi, query)
                           || SearchTextNormalizer.Contains(item.YibfNo, query)
                           || SearchTextNormalizer.Contains(item.Belediye, query)
                           || SearchTextNormalizer.Contains(item.Muteahhit, query))
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

    public void ApplyProjectSelection(KarotEntry entry, ProjectCatalogEntry project)
    {
        entry.ProjectId = project.Id;
        FillIfEmpty(entry.AdaParsel, project.AdaParsel, value => entry.AdaParsel = value);
        FillIfEmpty(entry.YapiSahibi, project.YapiSahibi, value => entry.YapiSahibi = value);
        FillIfEmpty(entry.YibfNo, project.YibfNo, value => entry.YibfNo = value);
        FillIfEmpty(entry.Muteahhit, project.Muteahhit, value => entry.Muteahhit = value);
    }

    public void ApplyProjectSelection(TadilatEntry entry, ProjectCatalogEntry project)
    {
        entry.ProjectId = project.Id;
        FillIfEmpty(entry.JobName, ChooseRicherText(project.DisplayName, BuildOwnerParcelText(project)), value => entry.JobName = value);
    }

    public void ApplyProjectSelection(ActionEntry entry, ProjectCatalogEntry project)
    {
        entry.ProjectId = project.Id;
        var ownerParcel = ChooseRicherText(BuildOwnerParcelText(project), project.DisplayName);
        FillIfEmpty(entry.OwnerParcelText, ownerParcel, value => entry.OwnerParcelText = value);
        // WorkText bilinçli olarak doldurulmaz; yapılacak iş kullanıcının yazdığı metindir.
    }

    public void ApplyProjectSelection(MissingProjectEntry entry, ProjectCatalogEntry project)
    {
        entry.ProjectId = project.Id;
        FillIfEmpty(entry.AdaParsel, project.AdaParsel, value => entry.AdaParsel = value);
        FillIfEmpty(entry.YapiSahibi, project.YapiSahibi, value => entry.YapiSahibi = value);
    }

    public void ApplyProjectSelection(TaskItem entry, ProjectCatalogEntry project)
    {
        entry.ProjectId = project.Id;
        entry.IsSpecialJob = project.Kind == ProjectCatalogKind.Special;
        FillIfEmpty(entry.Title, project.DisplayName, value => entry.Title = value);
    }

    public void ApplyProjectSelection(YibfIsTakibiEntry entry, ProjectCatalogEntry project)
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

        FillIfEmpty(entry.JobName, ChooseRicherText(project.DisplayName, BuildOwnerParcelText(project)), value => entry.JobName = value);
    }

    public void ApplyProjectSelection(YibfAnaBilgiEntry entry, ProjectCatalogEntry project)
    {
        FillIfEmpty(entry.AdaParsel, project.AdaParsel, value => entry.AdaParsel = value);
        FillIfEmpty(entry.YapiSahibi, project.YapiSahibi, value => entry.YapiSahibi = value);
        FillIfEmpty(entry.YibfNo, project.YibfNo, value => entry.YibfNo = value);
        FillIfEmpty(entry.Idare, project.Belediye, value => entry.Idare = value);
        FillIfEmpty(entry.Muteahhit, project.Muteahhit, value => entry.Muteahhit = value);
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
    {
        var combined = BuildDisplayName(project.AdaParsel ?? string.Empty, project.YapiSahibi ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return combined;
        }

        return project.DisplayName?.Trim() ?? string.Empty;
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
