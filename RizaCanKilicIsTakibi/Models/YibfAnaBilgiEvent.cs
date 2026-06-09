using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class YibfAnaBilgiEvent : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private Guid _entryId;
    private DateTime? _eventDate;
    private string _description = string.Empty;
    private string _backgroundColor = string.Empty;
    private string _noteText = string.Empty;
    private int _displayOrder;

    public Guid Id { get => _id; set => SetProperty(ref _id, value); }
    public Guid EntryId { get => _entryId; set => SetProperty(ref _entryId, value); }
    public DateTime? EventDate { get => _eventDate; set => SetProperty(ref _eventDate, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string BackgroundColor { get => _backgroundColor; set => SetProperty(ref _backgroundColor, value); }
    public string NoteText { get => _noteText; set => SetProperty(ref _noteText, value); }
    public int DisplayOrder { get => _displayOrder; set => SetProperty(ref _displayOrder, value); }
}