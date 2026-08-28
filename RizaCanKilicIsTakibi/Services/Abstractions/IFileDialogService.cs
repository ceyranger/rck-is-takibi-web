namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IFileDialogService
{
    string? ShowSaveDialog(string title, string filter, string defaultExtension);
    string? ShowOpenDialog(string title, string filter, bool multiselect = false);
    string? ShowFolderDialog(string title);
}
