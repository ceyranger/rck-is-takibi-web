using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Views.Controls;

public sealed class ProjectPickerListItem
{
    public ProjectPickerListItem(ProjectCatalogEntry entry, string title, string subtitle)
    {
        Entry = entry;
        Title = title;
        Subtitle = subtitle;
    }

    public ProjectCatalogEntry Entry { get; }

    public string Title { get; }

    public string Subtitle { get; }
}
