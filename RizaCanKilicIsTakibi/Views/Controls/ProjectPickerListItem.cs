using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Views.Controls;

public sealed class ProjectPickerListItem
{
    public ProjectPickerListItem(ProjectCatalogEntry entry, string subtitle)
    {
        Entry = entry;
        Subtitle = subtitle;
    }

    public ProjectCatalogEntry Entry { get; }

    public string DisplayName => Entry.DisplayName;

    public string Subtitle { get; }
}
