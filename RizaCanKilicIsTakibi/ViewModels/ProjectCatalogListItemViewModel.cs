using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class ProjectCatalogListItemViewModel
{
    public ProjectCatalogListItemViewModel(ProjectCatalogEntry entry, string parentDisplayName)
    {
        Entry = entry;
        ParentDisplayName = parentDisplayName;
        KindLabel = ProjectCatalogKindLabels.ToLabel(entry.Kind);
        ActiveLabel = entry.IsActive ? "Aktif" : "Pasif";
    }

    public ProjectCatalogEntry Entry { get; }
    public string DisplayName => Entry.DisplayName;
    public string AdaParsel => Entry.AdaParsel;
    public string YapiSahibi => Entry.YapiSahibi;
    public string YibfNo => Entry.YibfNo;
    public string Belediye => Entry.Belediye;
    public string Muteahhit => Entry.Muteahhit;
    public string KindLabel { get; }
    public string ActiveLabel { get; }
    public string ParentDisplayName { get; }
}
