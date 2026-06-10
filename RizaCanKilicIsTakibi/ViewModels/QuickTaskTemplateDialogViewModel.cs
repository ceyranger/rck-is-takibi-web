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
    private const string DefaultGroupName = "Genel";
    private readonly IQuickTaskTemplateRepository _repository;
    private QuickTaskTemplateGroupViewModel? _selectedGroup;
    private QuickTaskTemplateSelectionViewModel? _selectedTemplate;
    private string _newGroupName = string.Empty;
    private string _newTemplateTitle = string.Empty;
    private string _validationMessage = string.Empty;
    private IReadOnlyList<string> _selectedTitles = Array.Empty<string>();

    public QuickTaskTemplateDialogViewModel(IQuickTaskTemplateRepository repository, IEnumerable<QuickTaskTemplate> templates)
    {
        _repository = repository;
        AllTemplates = new ObservableCollection<QuickTaskTemplateSelectionViewModel>(
            templates.OrderBy(template => template.GroupName)
                .ThenBy(template => template.SortOrder)
                .ThenBy(template => template.UpdatedAt)
                .Select(template => new QuickTaskTemplateSelectionViewModel(template.Clone())));
        Groups = new ObservableCollection<QuickTaskTemplateGroupViewModel>();
        Templates = new ObservableCollection<QuickTaskTemplateSelectionViewModel>();

        AllTemplates.CollectionChanged += OnAllTemplatesCollectionChanged;
        Templates.CollectionChanged += OnTemplatesCollectionChanged;
        foreach (var template in AllTemplates)
        {
            template.PropertyChanged += OnTemplatePropertyChanged;
        }

        AddGroupCommand = new RelayCommand(AddGroup);
        DeleteSelectedGroupCommand = new AsyncRelayCommand(DeleteSelectedGroupAsync, () => SelectedGroup is not null);
        AddTemplateCommand = new AsyncRelayCommand(AddTemplateAsync, () => SelectedGroup is not null);
        DeleteSelectedTemplateCommand = new AsyncRelayCommand(DeleteSelectedTemplateAsync, () => SelectedTemplate is not null);
        SelectAllCommand = new RelayCommand(SelectAll, () => Templates.Count > 0);
        ClearSelectionCommand = new RelayCommand(ClearSelection, () => Templates.Any(item => item.IsSelected));
        AddSelectedTasksCommand = new RelayCommand(AddSelectedTasks, () => Templates.Any(item => item.IsSelected));
        AddSelectedGroupTasksCommand = new RelayCommand(AddSelectedGroupTasks, () => Templates.Count > 0);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));

        RefreshGroups();
        SelectedGroup = Groups.FirstOrDefault();
    }

    public event EventHandler<bool>? RequestClose;

    private ObservableCollection<QuickTaskTemplateSelectionViewModel> AllTemplates { get; }

    public ObservableCollection<QuickTaskTemplateGroupViewModel> Groups { get; }

    public ObservableCollection<QuickTaskTemplateSelectionViewModel> Templates { get; }

    public QuickTaskTemplateGroupViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                RefreshVisibleTemplates();
                AddTemplateCommand.NotifyCanExecuteChanged();
                DeleteSelectedGroupCommand.NotifyCanExecuteChanged();
            }
        }
    }

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

    public string NewGroupName
    {
        get => _newGroupName;
        set => SetProperty(ref _newGroupName, value);
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

    public RelayCommand AddGroupCommand { get; }
    public AsyncRelayCommand DeleteSelectedGroupCommand { get; }
    public AsyncRelayCommand AddTemplateCommand { get; }
    public AsyncRelayCommand DeleteSelectedTemplateCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand AddSelectedTasksCommand { get; }
    public RelayCommand AddSelectedGroupTasksCommand { get; }
    public RelayCommand CancelCommand { get; }

    private void AddGroup()
    {
        var groupName = NormalizeGroupName(NewGroupName);
        if (string.IsNullOrWhiteSpace(groupName))
        {
            ValidationMessage = "Grup adı boş olamaz.";
            return;
        }

        var existing = Groups.FirstOrDefault(group => string.Equals(group.Name, groupName, StringComparison.CurrentCultureIgnoreCase));
        if (existing is not null)
        {
            SelectedGroup = existing;
            NewGroupName = string.Empty;
            ValidationMessage = string.Empty;
            return;
        }

        var groupViewModel = new QuickTaskTemplateGroupViewModel(groupName);
        Groups.Add(groupViewModel);
        SelectedGroup = groupViewModel;
        NewGroupName = string.Empty;
        ValidationMessage = string.Empty;
        RefreshCommandStates();
    }

    private async Task DeleteSelectedGroupAsync()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        var group = SelectedGroup;
        var templatesToDelete = AllTemplates
            .Where(item => string.Equals(item.GroupName, group.Name, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        foreach (var template in templatesToDelete)
        {
            await _repository.DeleteAsync(template.Id);
            template.PropertyChanged -= OnTemplatePropertyChanged;
            AllTemplates.Remove(template);
        }

        Groups.Remove(group);
        SelectedGroup = Groups.FirstOrDefault();
        ValidationMessage = string.Empty;
        RefreshGroups(preserveSelection: SelectedGroup?.Name);
        RefreshCommandStates();
    }

    private async Task AddTemplateAsync()
    {
        if (SelectedGroup is null)
        {
            ValidationMessage = "Önce bir grup oluşturun veya seçin.";
            return;
        }

        var title = NewTemplateTitle.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ValidationMessage = "İş başlığı boş olamaz.";
            return;
        }

        if (AllTemplates.Any(item =>
                string.Equals(item.GroupName, SelectedGroup.Name, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(item.Title.Trim(), title, StringComparison.CurrentCultureIgnoreCase)))
        {
            ValidationMessage = "Bu grupta aynı başlıkta bir iş zaten var.";
            return;
        }

        var template = new QuickTaskTemplate
        {
            Id = Guid.NewGuid(),
            GroupName = SelectedGroup.Name,
            Title = title,
            SortOrder = Templates.Count,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };

        await _repository.SaveAsync(template);
        var item = new QuickTaskTemplateSelectionViewModel(template) { IsSelected = true };
        item.PropertyChanged += OnTemplatePropertyChanged;
        AllTemplates.Add(item);
        SelectedTemplate = item;
        NewTemplateTitle = string.Empty;
        ValidationMessage = string.Empty;
        RefreshGroups(preserveSelection: SelectedGroup.Name);
        RefreshVisibleTemplates();
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
        AllTemplates.Remove(item);
        SelectedTemplate = Templates.FirstOrDefault();
        ValidationMessage = string.Empty;
        RefreshGroups(preserveSelection: SelectedGroup?.Name);
        RefreshVisibleTemplates();
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

        CompleteWithTitles(selected, "En az bir iş seçin.");
    }

    private void AddSelectedGroupTasks()
    {
        var selected = Templates
            .OrderBy(item => item.Template.SortOrder)
            .Select(item => item.Title.Trim())
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .ToList();

        CompleteWithTitles(selected, "Seçili grupta eklenecek iş yok.");
    }

    private void CompleteWithTitles(IReadOnlyList<string> titles, string emptyMessage)
    {
        if (titles.Count == 0)
        {
            ValidationMessage = emptyMessage;
            return;
        }

        _selectedTitles = titles;
        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, true);
    }

    private void RefreshGroups(string? preserveSelection = null)
    {
        preserveSelection ??= SelectedGroup?.Name;
        var names = AllTemplates
            .Select(item => NormalizeGroupName(item.GroupName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        foreach (var existingGroup in Groups.ToList())
        {
            if (!names.Contains(existingGroup.Name, StringComparer.CurrentCultureIgnoreCase)
                && AllTemplates.All(item => !string.Equals(item.GroupName, existingGroup.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                continue;
            }
        }

        var existingNames = Groups.Select(group => group.Name).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        foreach (var name in names)
        {
            if (!existingNames.Contains(name))
            {
                Groups.Add(new QuickTaskTemplateGroupViewModel(name));
            }
        }

        foreach (var group in Groups.ToList())
        {
            group.TemplateCount = AllTemplates.Count(item => string.Equals(item.GroupName, group.Name, StringComparison.CurrentCultureIgnoreCase));
        }

        if (Groups.Count == 0 && AllTemplates.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(preserveSelection))
        {
            SelectedGroup = Groups.FirstOrDefault(group => string.Equals(group.Name, preserveSelection, StringComparison.CurrentCultureIgnoreCase))
                ?? Groups.FirstOrDefault();
        }
        else if (SelectedGroup is null)
        {
            SelectedGroup = Groups.FirstOrDefault();
        }
    }

    private void RefreshVisibleTemplates()
    {
        Templates.Clear();
        if (SelectedGroup is null)
        {
            SelectedTemplate = null;
            RefreshCommandStates();
            return;
        }

        foreach (var template in AllTemplates
                     .Where(item => string.Equals(item.GroupName, SelectedGroup.Name, StringComparison.CurrentCultureIgnoreCase))
                     .OrderBy(item => item.Template.SortOrder)
                     .ThenBy(item => item.Template.UpdatedAt))
        {
            Templates.Add(template);
        }

        SelectedTemplate = Templates.FirstOrDefault();
        RefreshCommandStates();
    }

    private void OnAllTemplatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
    }

    private void OnTemplatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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
        AddSelectedGroupTasksCommand.NotifyCanExecuteChanged();
        AddTemplateCommand.NotifyCanExecuteChanged();
        DeleteSelectedTemplateCommand.NotifyCanExecuteChanged();
        DeleteSelectedGroupCommand.NotifyCanExecuteChanged();
    }

    private static string NormalizeGroupName(string groupName)
        => string.IsNullOrWhiteSpace(groupName) ? DefaultGroupName : groupName.Trim();
}
