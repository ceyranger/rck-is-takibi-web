using RizaCanKilicIsTakibi.Helpers;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class QuickTaskTemplateGroupViewModel : ViewModelBase
{
    private int _templateCount;

    public QuickTaskTemplateGroupViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public int TemplateCount
    {
        get => _templateCount;
        set => SetProperty(ref _templateCount, value);
    }
}
