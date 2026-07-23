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
        _noteText = noteText;
        _selectedApprovalStatus = YibfAnaBilgiApprovalStatuses.Normalize(approvalStatus);
        _selectedColor = backgroundColor;

        ColorOptions =
        [
            new YibfAnaBilgiEventColorOption(string.Empty, "Renk Yok"),
            new YibfAnaBilgiEventColorOption("#FFFF0000", "Kırmızı"),
            new YibfAnaBilgiEventColorOption("#FFFFA500", "Turuncu"),
            new YibfAnaBilgiEventColorOption("#FFFFFF00", "Sarı"),
            new YibfAnaBilgiEventColorOption("#FF92D050", "Yeşil"),
            new YibfAnaBilgiEventColorOption("#FF4F81BD", "Mavi"),
            new YibfAnaBilgiEventColorOption("#FFD9D9D9", "Gri")
        ];

        ApprovalStatusOptions = YibfAnaBilgiApprovalStatuses.DialogOptions;

        // Edit: keep existing color. Add with category: suggest default color if none chosen.
        if (string.IsNullOrWhiteSpace(_selectedColor))
        {
            ApplyDefaultColorFromApprovalStatus();
        }

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

            // Suggest default color for the category; user can still change it afterward.
            ApplyDefaultColorFromApprovalStatus();
        }
    }

    /// <summary>Color is always user-editable for every category.</summary>
    public bool IsColorEditable => true;

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SetTodayCommand { get; }

    private void ApplyDefaultColorFromApprovalStatus()
    {
        var color = YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(SelectedApprovalStatus);
        if (color is null)
        {
            return;
        }

        SelectedColor = color;
    }

    private void Save()
    {
        var status = YibfAnaBilgiApprovalStatuses.Normalize(SelectedApprovalStatus);
        RequestClose?.Invoke(this, new YibfAnaBilgiEventDialogResult
        {
            EventDate = EventDate,
            Description = Description.Trim(),
            BackgroundColor = SelectedColor,
            NoteText = NoteText.Trim(),
            ApprovalStatus = status
        });
    }
}
