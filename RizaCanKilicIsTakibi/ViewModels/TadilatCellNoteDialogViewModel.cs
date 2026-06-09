using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class TadilatCellNoteDialogViewModel : ViewModelBase
{
    private string _noteText;

    public TadilatCellNoteDialogViewModel(string noteText)
    {
        _noteText = noteText;
        SaveCommand = new RelayCommand(Save);
        DeleteCommand = new RelayCommand(Delete);
        CancelCommand = new RelayCommand(Cancel);
    }

    public event EventHandler<TadilatCellNoteDialogResult?>? RequestClose;

    public string NoteText
    {
        get => _noteText;
        set => SetProperty(ref _noteText, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand CancelCommand { get; }

    private void Save()
    {
        RequestClose?.Invoke(this, new TadilatCellNoteDialogResult
        {
            NoteText = NoteText.Trim()
        });
    }

    private void Delete()
    {
        RequestClose?.Invoke(this, new TadilatCellNoteDialogResult
        {
            DeleteRequested = true,
            NoteText = string.Empty
        });
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, null);
    }
}
