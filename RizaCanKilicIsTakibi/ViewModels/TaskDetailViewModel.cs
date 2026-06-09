using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class TaskDetailViewModel : ViewModelBase
{
    private TaskItem? _currentTask;
    private string _newNoteText = string.Empty;

    public TaskDetailViewModel()
    {
        AddNoteCommand = new RelayCommand(AddNote, CanAddNote);
        RemoveNoteCommand = new RelayCommand<TaskNote>(RemoveNote);
    }

    public event EventHandler? TaskChanged;

    public TaskItem? CurrentTask
    {
        get => _currentTask;
        set
        {
            if (SetProperty(ref _currentTask, value))
            {
                OnPropertyChanged(nameof(HasTask));
                AddNoteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasTask => CurrentTask is not null;

    public string NewNoteText
    {
        get => _newNoteText;
        set
        {
            if (SetProperty(ref _newNoteText, value))
            {
                AddNoteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public RelayCommand AddNoteCommand { get; }

    public RelayCommand<TaskNote> RemoveNoteCommand { get; }

    public void Close()
    {
        CurrentTask = null;
        NewNoteText = string.Empty;
    }

    private void AddNote()
    {
        if (CurrentTask is null || string.IsNullOrWhiteSpace(NewNoteText))
        {
            return;
        }

        CurrentTask.Notes.Add(new TaskNote
        {
            Text = NewNoteText.Trim(),
            CreatedAt = DateTime.Now
        });

        CurrentTask.UpdatedAt = DateTime.Now;
        NewNoteText = string.Empty;
        TaskChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool CanAddNote()
        => CurrentTask is not null && !string.IsNullOrWhiteSpace(NewNoteText);

    private void RemoveNote(TaskNote? note)
    {
        if (CurrentTask is null || note is null)
        {
            return;
        }

        CurrentTask.Notes.Remove(note);
        CurrentTask.UpdatedAt = DateTime.Now;
        TaskChanged?.Invoke(this, EventArgs.Empty);
    }
}
