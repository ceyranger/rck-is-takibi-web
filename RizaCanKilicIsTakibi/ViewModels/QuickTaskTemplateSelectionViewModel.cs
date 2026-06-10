using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class QuickTaskTemplateSelectionViewModel : ViewModelBase
{
    private bool _isSelected;

    public QuickTaskTemplateSelectionViewModel(QuickTaskTemplate template)
    {
        Template = template;
    }

    public QuickTaskTemplate Template { get; }

    public Guid Id => Template.Id;

    public string Title => Template.Title;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
