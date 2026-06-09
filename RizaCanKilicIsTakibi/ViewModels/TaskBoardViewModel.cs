using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class TaskBoardViewModel : ViewModelBase
{
    private readonly string _title;
    private readonly TaskBoardType _boardType;
    private readonly ObservableCollection<TaskItem> _tasks;
    private readonly IReadOnlyList<ColumnFilterViewModel> _columnFilters;
    private TaskItem? _selectedTask;
    private string _filterText = string.Empty;

    public TaskBoardViewModel(string title, TaskBoardType boardType)
    {
        _title = title;
        _boardType = boardType;
        _tasks = new ObservableCollection<TaskItem>();
        _tasks.CollectionChanged += OnTasksCollectionChanged;

        TasksView = CollectionViewSource.GetDefaultView(_tasks);
        TasksView.Filter = FilterTask;

        TitleColumnFilter = new ColumnFilterViewModel("Başlık", RefreshTasksView, ApplyColumnSort);
        DescriptionColumnFilter = new ColumnFilterViewModel("Açıklama", RefreshTasksView, ApplyColumnSort);
        _columnFilters = [TitleColumnFilter, DescriptionColumnFilter];

        ApplyDefaultSort();
        RefreshColumnFilters();
    }

    public event EventHandler<TaskItem?>? SelectedTaskChanged;

    public event EventHandler? TasksChanged;

    public string Title => _title;

    public TaskBoardType BoardType => _boardType;

    public ObservableCollection<TaskItem> Tasks => _tasks;

    public ICollectionView TasksView { get; }

    public ColumnFilterViewModel TitleColumnFilter { get; }

    public ColumnFilterViewModel DescriptionColumnFilter { get; }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                SelectedTaskChanged?.Invoke(this, _selectedTask);
            }
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                TasksView.Refresh();
            }
        }
    }

    public void ReplaceAll(IEnumerable<TaskItem> items)
    {
        _tasks.Clear();
        foreach (var item in items.OrderBy(task => task.SortOrder))
        {
            AttachTask(item);
            _tasks.Add(item);
        }

        NormalizeSortOrder();
        RefreshColumnFilters();
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddTask(TaskItem task)
    {
        AttachTask(task);
        _tasks.Add(task);
        NormalizeSortOrder();
        RefreshColumnFilters();
        SelectedTask = task;
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public void InsertTask(int index, TaskItem task)
    {
        AttachTask(task);
        _tasks.Insert(Math.Clamp(index, 0, _tasks.Count), task);
        NormalizeSortOrder();
        RefreshColumnFilters();
        SelectedTask = task;
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveTask(TaskItem task)
    {
        DetachTask(task);
        _tasks.Remove(task);
        if (SelectedTask == task)
        {
            SelectedTask = null;
        }

        NormalizeSortOrder();
        RefreshColumnFilters();
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NormalizeSortOrder()
    {
        for (var i = 0; i < _tasks.Count; i++)
        {
            _tasks[i].SortOrder = i;
        }

        TasksView.Refresh();
    }

    public int IndexOf(TaskItem task) => _tasks.IndexOf(task);

    private bool FilterTask(object item)
    {
        if (item is not TaskItem task)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        var query = FilterText.Trim();
        if (!(SearchTextNormalizer.Contains(task.Title, query)
              || SearchTextNormalizer.Contains(task.Description, query)
              || task.Notes.Any(note => SearchTextNormalizer.Contains(note.Text, query))))
        {
            return false;
        }

        return TitleColumnFilter.IsMatch(task.Title)
               && DescriptionColumnFilter.IsMatch(task.Description);
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (TaskItem item in e.OldItems)
            {
                DetachTask(item);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (TaskItem item in e.NewItems)
            {
                AttachTask(item);
            }
        }

        RefreshColumnFilters();
    }

    private void AttachTask(TaskItem task)
    {
        task.BoardType = BoardType;
        task.PropertyChanged -= OnTaskPropertyChanged;
        task.PropertyChanged += OnTaskPropertyChanged;
    }

    private void DetachTask(TaskItem task)
    {
        task.PropertyChanged -= OnTaskPropertyChanged;
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TaskItem.SortOrder) or nameof(TaskItem.UpdatedAt))
        {
            TasksView.Refresh();
        }

        if (e.PropertyName is nameof(TaskItem.Title) or nameof(TaskItem.Description))
        {
            RefreshColumnFilters();
        }

        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshColumnFilters()
    {
        TitleColumnFilter.SetAvailableValues(_tasks.Select(task => task.Title));
        DescriptionColumnFilter.SetAvailableValues(_tasks.Select(task => task.Description));
        RefreshTasksView();
    }

    private void RefreshTasksView()
    {
        TasksView.Refresh();
    }

    private void ApplyColumnSort(ColumnFilterViewModel activeFilter)
    {
        foreach (var filter in _columnFilters)
        {
            if (!ReferenceEquals(filter, activeFilter))
            {
                filter.ClearSortSilently();
            }
        }

        TasksView.SortDescriptions.Clear();

        if (TitleColumnFilter.SortDirection is { } titleDirection)
        {
            TasksView.SortDescriptions.Add(new SortDescription(nameof(TaskItem.Title), titleDirection));
            TasksView.SortDescriptions.Add(new SortDescription(nameof(TaskItem.UpdatedAt), ListSortDirection.Descending));
            return;
        }

        if (DescriptionColumnFilter.SortDirection is { } descriptionDirection)
        {
            TasksView.SortDescriptions.Add(new SortDescription(nameof(TaskItem.Description), descriptionDirection));
            TasksView.SortDescriptions.Add(new SortDescription(nameof(TaskItem.UpdatedAt), ListSortDirection.Descending));
            return;
        }

        ApplyDefaultSort();
    }

    private void ApplyDefaultSort()
    {
        TasksView.SortDescriptions.Clear();
        TasksView.SortDescriptions.Add(new SortDescription(nameof(TaskItem.SortOrder), ListSortDirection.Ascending));
        TasksView.SortDescriptions.Add(new SortDescription(nameof(TaskItem.UpdatedAt), ListSortDirection.Descending));
    }
}
