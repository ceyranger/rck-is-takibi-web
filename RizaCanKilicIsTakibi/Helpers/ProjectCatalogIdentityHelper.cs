using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Helpers;

public sealed record ProjectCatalogEffectiveIdentity(
    string AdaParsel,
    string YapiSahibi,
    string YibfNo,
    string Muteahhit,
    string Belediye);

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

    public static string BuildPickerSubtitle(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var kind = ProjectCatalogKindLabels.ToLabel(project.Kind);
        var identity = ResolveEffectiveIdentity(project, catalog);
        var detail = !string.IsNullOrWhiteSpace(identity.YapiSahibi)
            ? identity.YapiSahibi
            : identity.AdaParsel;

        return string.IsNullOrWhiteSpace(detail)
            ? kind
            : $"{kind} · {detail}";
    }

    private static string Coalesce(string current, string? candidate)
        => string.IsNullOrWhiteSpace(current) ? Trim(candidate) : current;

    private static string Trim(string? value)
        => value?.Trim() ?? string.Empty;
}
