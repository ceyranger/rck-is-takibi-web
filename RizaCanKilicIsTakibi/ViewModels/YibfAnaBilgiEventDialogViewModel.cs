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
        _ = backgroundColor;
        _eventDate = eventDate ?? DateTime.Today;
        _description = description;
        _noteText = noteText;
        _selectedApprovalStatus = YibfAnaBilgiApprovalStatuses.Normalize(approvalStatus);
        _selectedColor = YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(_selectedApprovalStatus);

        ApprovalStatusOptions = YibfAnaBilgiApprovalStatuses.DialogOptions;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
        SetTodayCommand = new RelayCommand(() => EventDate = DateTime.Today);
    }

    public event EventHandler<YibfAnaBilgiEventDialogResult?>? RequestClose;

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
        private set => SetProperty(ref _selectedColor, value);
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

            SelectedColor = YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(_selectedApprovalStatus);
        }
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SetTodayCommand { get; }

    private void Save()
    {
        var status = YibfAnaBilgiApprovalStatuses.Normalize(SelectedApprovalStatus);
        RequestClose?.Invoke(this, new YibfAnaBilgiEventDialogResult
        {
            EventDate = EventDate,
            Description = Description.Trim(),
            BackgroundColor = YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(status),
            NoteText = NoteText.Trim(),
            ApprovalStatus = status
        });
    }
}
