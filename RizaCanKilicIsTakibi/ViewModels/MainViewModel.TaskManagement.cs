using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed partial class MainViewModel
{
    private void AddTask(TaskBoardType boardType)
    {
        var board = GetBoard(boardType);
        FocusBoard(boardType);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Yeni iş",
            Description = string.Empty,
            BoardType = boardType,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            SortOrder = 0
        };

        var action = new DelegateUndoableAction(
            "Görev ekle",
            () => board.InsertTask(0, task),
            () => board.RemoveTask(task));

        _undoRedoService.Execute(action);
        _notificationService.ShowToast("Yeni görev eklendi.", ToastType.Success);
        MarkTaskDirty();
    }

    private void DeleteSelectedTask(bool showToast = true, bool requireConfirmation = true)
    {
        var board = _activeBoard;
        var selected = board.SelectedTask;
        if (selected is null)
        {
            return;
        }

        if (requireConfirmation && !_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Delete,
                Title = "Görevi Sil",
                Message = $"\"{selected.Title}\" görevi silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        var snapshot = selected.Clone();
        var index = board.IndexOf(selected);

        var action = new DelegateUndoableAction(
            "Görev sil",
            () => board.RemoveTask(selected),
            () => board.InsertTask(index, snapshot));

        _undoRedoService.Execute(action);
        if (showToast)
        {
            _notificationService.ShowToast("Görev silindi.", ToastType.Warning);
        }
        MarkTaskDirty();
    }

    private void DeleteTask(TaskItem? task)
    {
        if (task is null)
        {
            return;
        }

        var board = GetBoard(task.BoardType);
        board.SelectedTask = task;
        _activeBoard = board;
        DeleteSelectedTask();
    }

    private async Task DeleteActiveSelectionAsync()
    {
        switch (SelectedMainTab)
        {
            case MainNavigationTab.GenelIsTakibi:
                DeleteSelectedTask();
                break;
            case MainNavigationTab.Aksiyon:
                if (ActionModule.DeleteActionEntryCommand.CanExecute(null))
                {
                    await ActionModule.DeleteActionEntryCommand.ExecuteAsync(null);
                }

                break;
            case MainNavigationTab.EksikProje:
                if (MissingProjectModule.DeleteEntryCommand.CanExecute(MissingProjectModule.SelectedEntry))
                {
                    await MissingProjectModule.DeleteEntryCommand.ExecuteAsync(MissingProjectModule.SelectedEntry);
                }

                break;
            case MainNavigationTab.KarotTakibi:
                if (KarotModule.DeleteKarotEntryCommand.CanExecute(null))
                {
                    await KarotModule.DeleteKarotEntryCommand.ExecuteAsync(null);
                }

                break;
            case MainNavigationTab.TadilatTakibi:
                if (TadilatModule.DeleteEntryCommand.CanExecute(null))
                {
                    await TadilatModule.DeleteEntryCommand.ExecuteAsync(null);
                }

                break;
            case MainNavigationTab.YibfAnaBilgi:
                if (YibfModule.DeleteActiveSelectionCommand.CanExecute(null))
                {
                    await YibfModule.DeleteActiveSelectionCommand.ExecuteAsync(null);
                }

                break;
            case MainNavigationTab.YibfIsTakibi:
                if (YibfModule.DeleteIsTakibiEntryCommand.CanExecute(null))
                {
                    await YibfModule.DeleteIsTakibiEntryCommand.ExecuteAsync(null);
                }

                break;
            case MainNavigationTab.YibfBekleyenIsler:
                break;
        }
    }

    private bool CanMoveUp()
    {
        var selected = _activeBoard.SelectedTask;
        return selected is not null && _activeBoard.IndexOf(selected) > 0;
    }

    private bool CanMoveDown()
    {
        var selected = _activeBoard.SelectedTask;
        return selected is not null && _activeBoard.IndexOf(selected) >= 0 && _activeBoard.IndexOf(selected) < _activeBoard.Tasks.Count - 1;
    }

    private void MoveTask(int direction)
    {
        var board = _activeBoard;
        var selected = board.SelectedTask;
        if (selected is null)
        {
            return;
        }

        var oldIndex = board.IndexOf(selected);
        var newIndex = oldIndex + direction;
        if (newIndex < 0 || newIndex >= board.Tasks.Count)
        {
            return;
        }

        var action = new DelegateUndoableAction(
            "Görev sırası değiştir",
            () =>
            {
                board.Tasks.Move(oldIndex, newIndex);
                board.NormalizeSortOrder();
                selected.UpdatedAt = DateTime.Now;
                board.SelectedTask = selected;
            },
            () =>
            {
                board.Tasks.Move(newIndex, oldIndex);
                board.NormalizeSortOrder();
                selected.UpdatedAt = DateTime.Now;
                board.SelectedTask = selected;
            });

        _undoRedoService.Execute(action);
        MarkTaskDirty();
    }

    private void CopySelectedTask()
    {
        if (SelectedTask is null)
        {
            return;
        }

        _clipboardTask = SelectedTask.Clone();
        _notificationService.ShowToast("Görev panoya kopyalandı.", ToastType.Info);
        PasteTaskCommand.NotifyCanExecuteChanged();
    }

    private void CopyTaskFromContext(TaskItem? task)
    {
        if (task is null)
        {
            return;
        }

        var board = GetBoard(task.BoardType);
        board.SelectedTask = task;
        _activeBoard = board;
        CopySelectedTask();
    }

    private void PasteTask()
    {
        if (_clipboardTask is null)
        {
            return;
        }

        var board = _activeBoard;
        var paste = _clipboardTask.Clone();
        paste.Id = Guid.NewGuid();
        paste.CreatedAt = DateTime.Now;
        paste.UpdatedAt = DateTime.Now;
        paste.SortOrder = board.Tasks.Count;
        paste.BoardType = board.BoardType;

        var action = new DelegateUndoableAction(
            "Görev yapıştır",
            () => board.AddTask(paste),
            () => board.RemoveTask(paste));

        _undoRedoService.Execute(action);
        _notificationService.ShowToast("Görev yapıştırıldı.", ToastType.Success);
        MarkTaskDirty();
    }

    private void PasteTaskToBoard(TaskBoardType boardType)
    {
        FocusBoard(boardType);
        PasteTask();
    }

    private void MoveTaskToBoard(DragDropTaskMoveRequest? request)
    {
        if (request is null)
        {
            return;
        }

        var task = request.Task;
        var source = GetBoard(task.BoardType);
        var target = GetBoard(request.TargetBoard);

        if (source == target)
        {
            return;
        }

        var sourceIndex = source.IndexOf(task);
        if (sourceIndex < 0)
        {
            return;
        }

        var action = new DelegateUndoableAction(
            "Panolar arasında taşı",
            () =>
            {
                source.RemoveTask(task);
                task.BoardType = target.BoardType;
                task.SortOrder = target.Tasks.Count;
                task.UpdatedAt = DateTime.Now;
                target.AddTask(task);
                FocusBoard(target.BoardType);
            },
            () =>
            {
                target.RemoveTask(task);
                task.BoardType = source.BoardType;
                task.UpdatedAt = DateTime.Now;
                source.InsertTask(sourceIndex, task);
                FocusBoard(source.BoardType);
            });

        _undoRedoService.Execute(action);
        _notificationService.ShowToast("Görev taşındı.", ToastType.Info);
        MarkTaskDirty();
    }

    private void CommitGeneralEdit()
    {
        MarkTaskDirty();
    }

    private void MarkTaskDirty()
    {
        if (_suppressTaskDirtyTracking)
        {
            return;
        }

        HasUnsavedChanges = true;
    }

    private void FocusBoard(TaskBoardType boardType)
    {
        _activeBoard = GetBoard(boardType);
        NotifySelectionCommands();
    }

    private TaskBoardViewModel GetBoard(TaskBoardType boardType)
        => boardType == TaskBoardType.Acil ? UrgentBoard : GeneralBoard;
}
