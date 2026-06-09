using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class ColumnFilterViewModel : ViewModelBase
{
    private readonly Action _filterChanged;
    private readonly Action<ColumnFilterViewModel> _sortChanged;
    private readonly Dictionary<string, bool> _selectedByValue = new(StringComparer.OrdinalIgnoreCase);
    private bool _isSyncing;
    private string _searchText = string.Empty;
    private bool _includeBlanks = true;
    private ListSortDirection? _sortDirection;

    public ColumnFilterViewModel(string header, Action filterChanged, Action<ColumnFilterViewModel> sortChanged)
    {
        Header = header;
        _filterChanged = filterChanged;
        _sortChanged = sortChanged;

        Options = [];
        OptionsView = CollectionViewSource.GetDefaultView(Options);
        OptionsView.Filter = FilterOption;

        SelectAllCommand = new RelayCommand(SelectAll);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        SortAscendingCommand = new RelayCommand(() => SetSortDirection(ListSortDirection.Ascending));
        SortDescendingCommand = new RelayCommand(() => SetSortDirection(ListSortDirection.Descending));
        ClearSortCommand = new RelayCommand(() => SetSortDirection(null));
    }

    public string Header { get; }

    public ObservableCollection<ColumnFilterOptionViewModel> Options { get; }

    public ICollectionView OptionsView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OptionsView.Refresh();
            }
        }
    }

    public bool IncludeBlanks
    {
        get => _includeBlanks;
        set
        {
            if (SetProperty(ref _includeBlanks, value))
            {
                OnPropertyChanged(nameof(HasActiveFilter));
                OnPropertyChanged(nameof(IsFilteredOrSorted));
                TriggerFilterChanged();
            }
        }
    }

    public ListSortDirection? SortDirection
    {
        get => _sortDirection;
        private set
        {
            if (SetProperty(ref _sortDirection, value))
            {
                OnPropertyChanged(nameof(IsFilteredOrSorted));
            }
        }
    }

    public bool HasActiveFilter
        => !IncludeBlanks || Options.Any(option => !option.IsSelected);

    public bool IsFilteredOrSorted => HasActiveFilter || SortDirection is not null;

    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand SortAscendingCommand { get; }
    public RelayCommand SortDescendingCommand { get; }
    public RelayCommand ClearSortCommand { get; }

    public void SetAvailableValues(IEnumerable<string?> values)
    {
        var normalizedValues = values
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _isSyncing = true;
        try
        {
            foreach (var option in Options)
            {
                _selectedByValue[option.Value] = option.IsSelected;
                option.PropertyChanged -= OnOptionPropertyChanged;
            }

            var activeValues = normalizedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var staleValue in _selectedByValue.Keys.Where(key => !activeValues.Contains(key)).ToList())
            {
                _selectedByValue.Remove(staleValue);
            }

            Options.Clear();
            foreach (var value in normalizedValues)
            {
                var option = new ColumnFilterOptionViewModel(value, _selectedByValue.TryGetValue(value, out var isSelected) ? isSelected : true);
                option.PropertyChanged += OnOptionPropertyChanged;
                Options.Add(option);
            }
        }
        finally
        {
            _isSyncing = false;
        }

        OptionsView.Refresh();
        OnPropertyChanged(nameof(HasActiveFilter));
        OnPropertyChanged(nameof(IsFilteredOrSorted));
    }

    public bool IsMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return IncludeBlanks;
        }

        var normalized = value.Trim();
        var option = Options.FirstOrDefault(item => string.Equals(item.Value, normalized, StringComparison.OrdinalIgnoreCase));
        return option?.IsSelected ?? true;
    }

    public void ClearSortSilently()
    {
        SortDirection = null;
    }

    private void SelectAll()
    {
        _isSyncing = true;
        try
        {
            foreach (var option in Options)
            {
                option.IsSelected = true;
            }

            IncludeBlanks = true;
        }
        finally
        {
            _isSyncing = false;
        }

        OnPropertyChanged(nameof(HasActiveFilter));
        OnPropertyChanged(nameof(IsFilteredOrSorted));
        TriggerFilterChanged();
    }

    private void ClearSelection()
    {
        _isSyncing = true;
        try
        {
            foreach (var option in Options)
            {
                option.IsSelected = false;
            }

            IncludeBlanks = false;
        }
        finally
        {
            _isSyncing = false;
        }

        OnPropertyChanged(nameof(HasActiveFilter));
        OnPropertyChanged(nameof(IsFilteredOrSorted));
        TriggerFilterChanged();
    }

    private void SetSortDirection(ListSortDirection? direction)
    {
        SortDirection = direction;
        _sortChanged(this);
    }

    private bool FilterOption(object item)
    {
        if (item is not ColumnFilterOptionViewModel option)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return SearchTextNormalizer.Contains(option.Value, SearchText);
    }

    private void OnOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncing || e.PropertyName != nameof(ColumnFilterOptionViewModel.IsSelected))
        {
            return;
        }

        OnPropertyChanged(nameof(HasActiveFilter));
        OnPropertyChanged(nameof(IsFilteredOrSorted));
        TriggerFilterChanged();
    }

    private void TriggerFilterChanged()
    {
        if (_isSyncing)
        {
            return;
        }

        _filterChanged();
    }
}

public sealed class ColumnFilterOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public ColumnFilterOptionViewModel(string value, bool isSelected)
    {
        Value = value;
        _isSelected = isSelected;
    }

    public string Value { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
