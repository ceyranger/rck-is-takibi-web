using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class YibfAnaBilgiEventDialogViewModel : ViewModelBase
{
    private DateTime? _eventDate;
    private string _description;
    private string _selectedColor;
    private string _noteText;

    public YibfAnaBilgiEventDialogViewModel(DateTime? eventDate, string description, string backgroundColor, string noteText)
    {
        _eventDate = eventDate;
        _description = description;
        _selectedColor = backgroundColor;
        _noteText = noteText;

        ColorOptions =
        [
            new YibfAnaBilgiEventColorOption(string.Empty, "Renk Yok"),
            new YibfAnaBilgiEventColorOption("#FFFF0000", "Kırmızı"),
            new YibfAnaBilgiEventColorOption("#FFFFFF00", "Sarı"),
            new YibfAnaBilgiEventColorOption("#FF92D050", "Yeşil"),
            new YibfAnaBilgiEventColorOption("#FF4F81BD", "Mavi"),
            new YibfAnaBilgiEventColorOption("#FFD9D9D9", "Gri")
        ];

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
    }

    public event EventHandler<YibfAnaBilgiEventDialogResult?>? RequestClose;

    public IReadOnlyList<YibfAnaBilgiEventColorOption> ColorOptions { get; }

    public DateTime? EventDate
    {
        get => _eventDate;
        set => SetProperty(ref _eventDate, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string SelectedColor
    {
        get => _selectedColor;
        set => SetProperty(ref _selectedColor, value);
    }

    public string NoteText
    {
        get => _noteText;
        set => SetProperty(ref _noteText, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    private void Save()
    {
        RequestClose?.Invoke(this, new YibfAnaBilgiEventDialogResult
        {
            EventDate = EventDate,
            Description = Description.Trim(),
            BackgroundColor = SelectedColor,
            NoteText = NoteText.Trim()
        });
    }
}
