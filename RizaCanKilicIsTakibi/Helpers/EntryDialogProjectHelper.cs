using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Helpers;

public static class EntryDialogProjectHelper
{
    public static bool IsOwnerParcelIncomplete(ProjectCatalogEntry? project)
    {
        if (project is null)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(project.AdaParsel)
               && string.IsNullOrWhiteSpace(project.YapiSahibi);
    }

    public static string BuildOwnerParcelSummary(ProjectCatalogEntry project)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(project.AdaParsel))
        {
            parts.Add(project.AdaParsel.Trim());
        }

        if (!string.IsNullOrWhiteSpace(project.YapiSahibi))
        {
            parts.Add(project.YapiSahibi.Trim());
        }

        if (!string.IsNullOrWhiteSpace(project.YibfNo))
        {
            parts.Add($"YİBF {project.YibfNo.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(project.Muteahhit))
        {
            parts.Add(project.Muteahhit.Trim());
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
