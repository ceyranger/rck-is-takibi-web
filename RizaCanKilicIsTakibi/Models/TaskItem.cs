using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class TaskItem : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private string _title = string.Empty;
    private string _description = string.Empty;
    private DateTime? _dueDate;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;
    private TaskBoardType _boardType = TaskBoardType.Genel;
    private int _sortOrder;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    public TaskBoardType BoardType
    {
        get => _boardType;
        set => SetProperty(ref _boardType, value);
    }

    public int SortOrder
    {
        get => _sortOrder;
        set => SetProperty(ref _sortOrder, value);
    }

    public ObservableCollection<TaskNote> Notes { get; } = new();

    public TaskItem Clone()
    {
        var clone = new TaskItem
        {
            Id = Id,
            Title = Title,
            Description = Description,
            DueDate = DueDate,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            BoardType = BoardType,
            SortOrder = SortOrder
        };

        foreach (var note in Notes)
        {
            clone.Notes.Add(new TaskNote
            {
                Id = note.Id,
                Text = note.Text,
                CreatedAt = note.CreatedAt
            });
        }

        return clone;
    }
}
