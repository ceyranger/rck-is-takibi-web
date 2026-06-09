using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class AddActionEntryDialogViewModel : ViewModelBase
{
    private string _ownerParcelText = string.Empty;
    private string _workText = string.Empty;
    private string _validationMessage = string.Empty;

    public AddActionEntryDialogViewModel(string district, ActionEntryCategory category)
    {
        District = district;
        Category = category;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string District { get; }

    public ActionEntryCategory Category { get; }

    public string OwnerParcelText
    {
        get => _ownerParcelText;
        set => SetProperty(ref _ownerParcelText, value);
    }

    public string WorkText
    {
        get => _workText;
        set => SetProperty(ref _workText, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public ActionEntry BuildEntry(int displayOrder)
    {
        return new ActionEntry
        {
            Id = Guid.NewGuid(),
            Category = Category,
            District = District,
            OwnerParcelText = OwnerParcelText.Trim(),
            WorkText = WorkText.Trim(),
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(OwnerParcelText) || string.IsNullOrWhiteSpace(WorkText))
        {
            ValidationMessage = "Ada/Parsel/Yapı Sahibi ve Yapılacak İş alanları zorunludur.";
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, true);
    }
}
