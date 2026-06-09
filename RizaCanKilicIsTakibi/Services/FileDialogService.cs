using Microsoft.Win32;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? ShowSaveDialog(string title, string filter, string defaultExtension)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            DefaultExt = defaultExtension,
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowOpenDialog(string title, string filter, bool multiselect = false)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = multiselect,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
