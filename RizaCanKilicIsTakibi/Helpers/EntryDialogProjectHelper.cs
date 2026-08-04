using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Helpers;

public static class EntryDialogProjectHelper
{
    public static bool IsOwnerParcelIncomplete(
        ProjectCatalogEntry? project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        if (project is null)
        {
            return false;
        }

        var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(project, catalog);
        return string.IsNullOrWhiteSpace(identity.AdaParsel)
               && string.IsNullOrWhiteSpace(identity.YapiSahibi);
    }

    public static string BuildOwnerParcelSummary(
        ProjectCatalogEntry project,
        IEnumerable<ProjectCatalogEntry>? catalog = null)
    {
        var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(project, catalog);
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(identity.AdaParsel))
        {
            parts.Add(identity.AdaParsel);
        }

        if (!string.IsNullOrWhiteSpace(identity.YapiSahibi))
        {
            parts.Add(identity.YapiSahibi);
        }

        if (!string.IsNullOrWhiteSpace(identity.YibfNo))
        {
            parts.Add($"YİBF {identity.YibfNo}");
        }

        if (!string.IsNullOrWhiteSpace(identity.Muteahhit))
        {
            parts.Add(identity.Muteahhit);
        }

        if (parts.Count == 0 && !string.IsNullOrWhiteSpace(project.DisplayName))
        {
            parts.Add(project.DisplayName.Trim());
        }

        return parts.Count == 0
            ? "Proje seçildi; kimlik bilgisi eksik."
            : $"Proje bilgisi: {string.Join(" · ", parts)}";
    }

    public static string BuildJobSummary(ProjectCatalogEntry project, string jobName)
    {
        var label = string.IsNullOrWhiteSpace(jobName) ? project.DisplayName?.Trim() : jobName.Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            return "Proje seçildi; iş adı eksik.";
        }

        return $"Proje bilgisi: {label}";
    }
}
