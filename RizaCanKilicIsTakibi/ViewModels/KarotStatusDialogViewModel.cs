using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class KarotStatusDialogViewModel : ViewModelBase
{
    private KarotStatus _selectedStatus;

    public KarotStatusDialogViewModel(KarotStatus currentStatus)
    {
        _selectedStatus = currentStatus;
        StatusOptions =
        [
            new KarotStatusOption(KarotStatus.KarotAlinacak, "Karot Alınacak"),
            new KarotStatusOption(KarotStatus.KarotAlindiSonucBekleniyor, "Karot Alındı Sonuç Bekleniyor"),
            new KarotStatusOption(KarotStatus.KarotAlindiOlumlu, "Karot Alındı Olumlu"),
            new KarotStatusOption(KarotStatus.KarotAlindiOlumsuz, "Karot Alındı Olumsuz")
        ];

        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public IReadOnlyList<KarotStatusOption> StatusOptions { get; }

    public KarotStatus SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public RelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }
}
