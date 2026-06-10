using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class QuickTaskTemplateDialogViewModel : ViewModelBase
{
    private readonly IQuickTaskTemplateRepository _repository;
    private QuickTaskTemplateSelectionViewModel? _selectedTemplate;
    private string _newTemplateTitle = string.Empty;
    private string _validationMessage = string.Empty;
    private IReadOnlyList<string> _selectedTitles = Array.Empty<string>();

    public QuickTaskTemplateDialogViewModel(IQuickTaskTemplateRepository repository, IEnumerable<QuickTaskTemplate> templates)
    {
        _repository = repository;
        Templates = new ObservableCollection<QuickTaskTemplateSelectionViewModel>(
            templates.OrderBy(template => template.SortOrder)
                .ThenBy(template => template.UpdatedAt)
                .Select(template => new QuickTaskTemplateSelectionViewModel(template.Clone())));

        Templates.CollectionChanged += OnTemplatesCollectionChanged;
        foreach (var template in Templates)
        {
            template.PropertyChanged += OnTemplatePropertyChanged;
        }

        AddTemplateCommand = new AsyncRelayCommand(AddTemplateAsync);
        DeleteSelectedTemplateCommand = new AsyncRelayCommand(DeleteSelectedTemplateAsync, () => SelectedTemplate is not null);
        SelectAllCommand = new RelayCommand(SelectAll, () => Templates.Count > 0);
        ClearSelectionCommand = new RelayCommand(ClearSelection, () => Templates.Any(item => item.IsSelected));
        AddSelectedTasksCommand = new RelayCommand(AddSelectedTasks, () => Templates.Any(item => item.IsSelected));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<QuickTaskTemplateSelectionViewModel> Templates { get; }

    public QuickTaskTemplateSelectionViewModel? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetProperty(ref _selectedTemplate, value))
            {
                DeleteSelectedTemplateCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewTemplateTitle
    {
        get => _newTemplateTitle;
        set => SetProperty(ref _newTemplateTitle, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public IReadOnlyList<string> SelectedTitles => _selectedTitles;

    public AsyncRelayCommand AddTemplateCommand { get; }
    public AsyncRelayCommand DeleteSelectedTemplateCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand AddSelectedTasksCommand { get; }
    public RelayCommand CancelCommand { get; }

    private async Task AddTemplateAsync()
    {
        var title = NewTemplateTitle.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ValidationMessage = "Şablon başlığı boş olamaz.";
            return;
        }

        if (Templates.Any(item => string.Equals(item.Title.Trim(), title, StringComparison.CurrentCultureIgnoreCase)))
        {
            ValidationMessage = "Aynı başlıkta bir şablon zaten var.";
            return;
        }

        var template = new QuickTaskTemplate
        {
            Id = Guid.NewGuid(),
            Title = title,
            SortOrder = Templates.Count,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };

        await _repository.SaveAsync(template);
        var item = new QuickTaskTemplateSelectionViewModel(template) { IsSelected = true };
        item.PropertyChanged += OnTemplatePropertyChanged;
        Templates.Add(item);
        SelectedTemplate = item;
        NewTemplateTitle = string.Empty;
        ValidationMessage = string.Empty;
        RefreshCommandStates();
    }

    private async Task DeleteSelectedTemplateAsync()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        var item = SelectedTemplate;
        await _repository.DeleteAsync(item.Id);
        item.PropertyChanged -= OnTemplatePropertyChanged;
        Templates.Remove(item);
        SelectedTemplate = Templates.FirstOrDefault();
        ValidationMessage = string.Empty;
        RefreshCommandStates();
    }

    private void SelectAll()
    {
        foreach (var item in Templates)
        {
            item.IsSelected = true;
        }

        RefreshCommandStates();
    }

    private void ClearSelection()
    {
        foreach (var item in Templates)
        {
            item.IsSelected = false;
        }

        RefreshCommandStates();
    }

    private void AddSelectedTasks()
    {
        var selected = Templates
            .Where(item => item.IsSelected)
            .OrderBy(item => item.Template.SortOrder)
            .Select(item => item.Title.Trim())
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .ToList();

        if (selected.Count == 0)
        {
            ValidationMessage = "En az bir şablon seçin.";
            return;
        }

        _selectedTitles = selected;
        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, true);
    }

    private void OnTemplatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (QuickTaskTemplateSelectionViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnTemplatePropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (QuickTaskTemplateSelectionViewModel item in e.NewItems)
            {
                item.PropertyChanged -= OnTemplatePropertyChanged;
                item.PropertyChanged += OnTemplatePropertyChanged;
            }
        }

        RefreshCommandStates();
    }

    private void OnTemplatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuickTaskTemplateSelectionViewModel.IsSelected))
        {
            RefreshCommandStates();
        }
    }

    private void RefreshCommandStates()
    {
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        AddSelectedTasksCommand.NotifyCanExecuteChanged();
        DeleteSelectedTemplateCommand.NotifyCanExecuteChanged();
    }
}
