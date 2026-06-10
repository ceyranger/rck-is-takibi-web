using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class QuickTaskTemplate : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private string _groupName = string.Empty;
    private string _title = string.Empty;
    private int _sortOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;
    private bool _isDeleted;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string GroupName
    {
        get => _groupName;
        set => SetProperty(ref _groupName, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public int SortOrder
    {
        get => _sortOrder;
        set => SetProperty(ref _sortOrder, value);
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

    public bool IsDeleted
    {
        get => _isDeleted;
        set => SetProperty(ref _isDeleted, value);
    }

    public QuickTaskTemplate Clone()
        => new()
        {
            Id = Id,
            GroupName = GroupName,
            Title = Title,
            SortOrder = SortOrder,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            IsDeleted = IsDeleted
        };
}
