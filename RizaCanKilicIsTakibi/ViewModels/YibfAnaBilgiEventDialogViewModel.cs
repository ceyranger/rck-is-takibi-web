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
    private string _selectedApprovalStatus;

    public YibfAnaBilgiEventDialogViewModel(
        DateTime? eventDate,
        string description,
        string backgroundColor,
        string noteText,
        string approvalStatus = "")
    {
        _eventDate = eventDate ?? DateTime.Today;
        _description = description;
        _selectedColor = backgroundColor;
        _noteText = noteText;
        _selectedApprovalStatus = YibfAnaBilgiApprovalStatuses.Normalize(approvalStatus);

        ColorOptions =
        [
            new YibfAnaBilgiEventColorOption(string.Empty, "Renk Yok"),
            new YibfAnaBilgiEventColorOption("#FFFF0000", "Kırmızı"),
            new YibfAnaBilgiEventColorOption("#FFFFFF00", "Sarı"),
            new YibfAnaBilgiEventColorOption("#FF92D050", "Yeşil"),
            new YibfAnaBilgiEventColorOption("#FF4F81BD", "Mavi"),
            new YibfAnaBilgiEventColorOption("#FFD9D9D9", "Gri")
        ];

        ApprovalStatusOptions = YibfAnaBilgiApprovalStatuses.DialogOptions;
        ApplyColorFromApprovalStatus();

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
        SetTodayCommand = new RelayCommand(() => EventDate = DateTime.Today);
    }

    public event EventHandler<YibfAnaBilgiEventDialogResult?>? RequestClose;

    public IReadOnlyList<YibfAnaBilgiEventColorOption> ColorOptions { get; }
    public IReadOnlyList<YibfAnaBilgiApprovalStatusOption> ApprovalStatusOptions { get; }

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

    public string SelectedApprovalStatus
    {
        get => _selectedApprovalStatus;
        set
        {
            if (!SetProperty(ref _selectedApprovalStatus, YibfAnaBilgiApprovalStatuses.Normalize(value)))
            {
                return;
            }

            ApplyColorFromApprovalStatus();
            OnPropertyChanged(nameof(IsColorEditable));
        }
    }

    public bool IsColorEditable
        => string.IsNullOrWhiteSpace(SelectedApprovalStatus);

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SetTodayCommand { get; }

    private void ApplyColorFromApprovalStatus()
    {
        var color = YibfAnaBilgiApprovalStatuses.GetColorForStatus(SelectedApprovalStatus);
        if (color is null)
        {
            return;
        }

        SelectedColor = color;
    }

    private void Save()
    {
        var status = YibfAnaBilgiApprovalStatuses.Normalize(SelectedApprovalStatus);
        var color = YibfAnaBilgiApprovalStatuses.GetColorForStatus(status) ?? SelectedColor;
        RequestClose?.Invoke(this, new YibfAnaBilgiEventDialogResult
        {
            EventDate = EventDate,
            Description = Description.Trim(),
            BackgroundColor = color,
            NoteText = NoteText.Trim(),
            ApprovalStatus = status
        });
    }
}
