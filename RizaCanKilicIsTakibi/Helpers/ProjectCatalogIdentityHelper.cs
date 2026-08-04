using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Helpers;

public sealed record ProjectCatalogEffectiveIdentity(
    string AdaParsel,
    string YapiSahibi,
    string YibfNo,
    string Muteahhit,
    string Belediye);

public readonly record struct ProjectPickerSortKey(
    string OwnerKey,
    string AdaKey,
    int KindOrder,
    int DisplayOrder,
    string Title) : IComparable<ProjectPickerSortKey>
{
    public int CompareTo(ProjectPickerSortKey other)
    {
        var owner = string.CompareOrdinal(OwnerKey, other.OwnerKey);
        if (owner != 0)
        {
            return owner;
        }

        var ada = string.CompareOrdinal(AdaKey, other.AdaKey);
        if (ada != 0)
        {
            return ada;
        }

        var kind = KindOrder.CompareTo(other.KindOrder);
        if (kind != 0)
        {
            return kind;
        }

        var order = DisplayOrder.CompareTo(other.DisplayOrder);
        if (order != 0)
        {
            return order;
        }

        return string.CompareOrdinal(Title, other.Title);
    }
}

public static class ProjectCatalogIdentityHelper
{
    public static ProjectCatalogEffectiveIdentity ResolveEffectiveIdentity(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var ada = Trim(project.AdaParsel);
        var sahip = Trim(project.YapiSahibi);
        var yibf = Trim(project.YibfNo);
        var muteahhit = Trim(project.Muteahhit);
        var belediye = Trim(project.Belediye);

        if (project.Kind == ProjectCatalogKind.Istinat
            && project.ParentProjectId is Guid parentId
            && parentId != Guid.Empty
            && catalog is not null)
        {
            var parent = catalog.FirstOrDefault(item => item.Id == parentId);
            if (parent is not null)
            {
                ada = Coalesce(ada, parent.AdaParsel);
                sahip = Coalesce(sahip, parent.YapiSahibi);
                yibf = Coalesce(yibf, parent.YibfNo);
                muteahhit = Coalesce(muteahhit, parent.Muteahhit);
                belediye = Coalesce(belediye, parent.Belediye);
            }
        }

        if ((project.Kind == ProjectCatalogKind.Normal || project.Kind == ProjectCatalogKind.Istinat)
            && string.IsNullOrWhiteSpace(muteahhit)
            && !string.IsNullOrWhiteSpace(sahip))
        {
            muteahhit = sahip;
        }

        return new ProjectCatalogEffectiveIdentity(ada, sahip, yibf, muteahhit, belediye);
    }

    public static bool MatchesSearch(
        ProjectCatalogEntry project,
        string? query,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        if (SearchTextNormalizer.Contains(project.DisplayName, query)
            || SearchTextNormalizer.Contains(project.AdaParsel, query)
            || SearchTextNormalizer.Contains(project.YapiSahibi, query)
            || SearchTextNormalizer.Contains(project.YibfNo, query)
            || SearchTextNormalizer.Contains(project.Muteahhit, query)
            || SearchTextNormalizer.Contains(project.Belediye, query))
        {
            return true;
        }

        var identity = ResolveEffectiveIdentity(project, catalog);
        if (SearchTextNormalizer.Contains(identity.AdaParsel, query)
            || SearchTextNormalizer.Contains(identity.YapiSahibi, query)
            || SearchTextNormalizer.Contains(identity.YibfNo, query)
            || SearchTextNormalizer.Contains(identity.Muteahhit, query)
            || SearchTextNormalizer.Contains(identity.Belediye, query))
        {
            return true;
        }

        var title = BuildPickerTitle(project, catalog);
        if (SearchTextNormalizer.Contains(title, query))
        {
            return true;
        }

        if (project.Kind == ProjectCatalogKind.Istinat
            && project.ParentProjectId is Guid parentId
            && parentId != Guid.Empty
            && catalog is not null)
        {
            var parent = catalog.FirstOrDefault(item => item.Id == parentId);
            if (parent is not null && SearchTextNormalizer.Contains(parent.DisplayName, query))
            {
                return true;
            }
        }

        return false;
    }

    public static string BuildPickerTitle(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var identity = ResolveEffectiveIdentity(project, catalog);
        var detail = !string.IsNullOrWhiteSpace(identity.YapiSahibi)
            ? identity.YapiSahibi
            : identity.AdaParsel;

        if (project.Kind == ProjectCatalogKind.Istinat)
        {
            return string.IsNullOrWhiteSpace(detail)
                ? "İstinat"
                : $"İstinat · {detail}";
        }

        var displayName = Trim(project.DisplayName);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return string.IsNullOrWhiteSpace(detail) ? string.Empty : detail;
    }

    public static string BuildPickerSubtitle(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var kind = ProjectCatalogKindLabels.ToLabel(project.Kind);
        if (project.Kind == ProjectCatalogKind.Istinat)
        {
            return kind;
        }

        var identity = ResolveEffectiveIdentity(project, catalog);
        if (!string.IsNullOrWhiteSpace(identity.AdaParsel)
            && !string.IsNullOrWhiteSpace(identity.YibfNo))
        {
            return $"{kind} · {identity.AdaParsel} · YİBF {identity.YibfNo}";
        }

        if (!string.IsNullOrWhiteSpace(identity.AdaParsel))
        {
            return $"{kind} · {identity.AdaParsel}";
        }

        if (!string.IsNullOrWhiteSpace(identity.YibfNo))
        {
            return $"{kind} · YİBF {identity.YibfNo}";
        }

        return kind;
    }

    public static ProjectPickerSortKey GetPickerSortKey(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var identity = ResolveEffectiveIdentity(project, catalog);
        var title = BuildPickerTitle(project, catalog);
        var kindOrder = project.Kind switch
        {
            ProjectCatalogKind.Normal => 0,
            ProjectCatalogKind.Istinat => 1,
            ProjectCatalogKind.Special => 2,
            _ => 9
        };

        return new ProjectPickerSortKey(
            SearchTextNormalizer.Normalize(identity.YapiSahibi),
            SearchTextNormalizer.Normalize(identity.AdaParsel),
            kindOrder,
            project.DisplayOrder,
            SearchTextNormalizer.Normalize(title));
    }

    public static string BuildEffectiveOwnerParcelText(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var identity = ResolveEffectiveIdentity(project, catalog);
        var parts = new[] { identity.AdaParsel, identity.YapiSahibi }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        var combined = string.Join(' ', parts);
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return combined;
        }

        return BuildPickerTitle(project, catalog);
    }

    private static string Coalesce(string current, string? candidate)
        => string.IsNullOrWhiteSpace(current) ? Trim(candidate) : current;

    private static string Trim(string? value)
        => value?.Trim() ?? string.Empty;
}
