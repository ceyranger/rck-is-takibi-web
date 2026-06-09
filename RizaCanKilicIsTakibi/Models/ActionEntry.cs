using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class ActionEntry : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private ActionEntryCategory _category;
    private string _district = string.Empty;
    private string _ownerParcelText = string.Empty;
    private string _workText = string.Empty;
    private int _displayOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public ActionEntryCategory Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string District
    {
        get => _district;
        set => SetProperty(ref _district, value);
    }

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

    public int DisplayOrder
    {
        get => _displayOrder;
        set => SetProperty(ref _displayOrder, value);
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
}
