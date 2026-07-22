using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RizaCanKilicIsTakibi.Views.Controls;

public partial class ProjectPickerControl : UserControl
{
    public static readonly DependencyProperty SelectedProjectIdProperty =
        DependencyProperty.Register(
            nameof(SelectedProjectId),
            typeof(Guid?),
            typeof(ProjectPickerControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedProjectIdChanged));

    public static readonly DependencyProperty CatalogProperty =
        DependencyProperty.Register(
            nameof(Catalog),
            typeof(IEnumerable),
            typeof(ProjectPickerControl),
            new PropertyMetadata(null, OnCatalogChanged));

    public static readonly DependencyProperty AllowClearProperty =
        DependencyProperty.Register(
            nameof(AllowClear),
            typeof(bool),
            typeof(ProjectPickerControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(
            nameof(SearchText),
            typeof(string),
            typeof(ProjectPickerControl),
            new PropertyMetadata(string.Empty, OnSearchTextChanged));

    public static readonly DependencyProperty IsPopupOpenProperty =
        DependencyProperty.Register(
            nameof(IsPopupOpen),
            typeof(bool),
            typeof(ProjectPickerControl),
            new PropertyMetadata(false));

    public static readonly RoutedEvent ProjectSelectedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ProjectSelected),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ProjectPickerControl));

    public ProjectPickerControl()
    {
        InitializeComponent();
        FilteredEntries = new ObservableCollection<ProjectCatalogEntry>();
    }

    public ObservableCollection<ProjectCatalogEntry> FilteredEntries { get; }

    public Guid? SelectedProjectId
    {
        get => (Guid?)GetValue(SelectedProjectIdProperty);
        set => SetValue(SelectedProjectIdProperty, value);
    }

    public IEnumerable? Catalog
    {
        get => (IEnumerable?)GetValue(CatalogProperty);
        set => SetValue(CatalogProperty, value);
    }

    public bool AllowClear
    {
        get => (bool)GetValue(AllowClearProperty);
        set => SetValue(AllowClearProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public bool IsPopupOpen
    {
        get => (bool)GetValue(IsPopupOpenProperty);
        set => SetValue(IsPopupOpenProperty, value);
    }

    public event RoutedEventHandler ProjectSelected
    {
        add => AddHandler(ProjectSelectedEvent, value);
        remove => RemoveHandler(ProjectSelectedEvent, value);
    }

    private static void OnSelectedProjectIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProjectPickerControl control)
        {
            control.UpdateSearchTextFromSelection();
        }
    }

    private static void OnCatalogChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProjectPickerControl control)
        {
            control.RefreshFilteredEntries();
        }
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProjectPickerControl control)
        {
            control.RefreshFilteredEntries();
            control.IsPopupOpen = true;
        }
    }

    private void UpdateSearchTextFromSelection()
    {
        if (SelectedProjectId is Guid projectId)
        {
            var match = EnumerateCatalog().FirstOrDefault(item => item.Id == projectId);
            SearchText = match?.DisplayName ?? string.Empty;
        }
        else if (AllowClear)
        {
            SearchText = string.Empty;
        }
    }

    private IEnumerable<ProjectCatalogEntry> EnumerateCatalog()
        => Catalog?.OfType<ProjectCatalogEntry>() ?? [];

    private void RefreshFilteredEntries()
    {
        FilteredEntries.Clear();
        var query = SearchText?.Trim() ?? string.Empty;
        var source = EnumerateCatalog().Where(item => item.IsActive);
        IEnumerable<ProjectCatalogEntry> matches = string.IsNullOrWhiteSpace(query)
            ? source.OrderBy(item => item.DisplayOrder).ThenBy(item => item.DisplayName)
            : source.Where(item =>
                SearchTextNormalizer.Contains(item.DisplayName, query)
                || SearchTextNormalizer.Contains(item.AdaParsel, query)
                || SearchTextNormalizer.Contains(item.YapiSahibi, query)
                || SearchTextNormalizer.Contains(item.YibfNo, query))
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.DisplayName);

        foreach (var item in matches.Take(40))
        {
            FilteredEntries.Add(item);
        }
    }

    private void OnSearchBoxGotFocus(object sender, RoutedEventArgs e)
    {
        RefreshFilteredEntries();
        IsPopupOpen = true;
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            IsPopupOpen = false;
            return;
        }

        if (e.Key == Key.Enter && FilteredEntries.Count > 0)
        {
            ApplySelection(FilteredEntries[0]);
            e.Handled = true;
        }
    }

    private void OnResultSelected(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox { SelectedItem: ProjectCatalogEntry entry })
        {
            ApplySelection(entry);
        }
    }

    private void ApplySelection(ProjectCatalogEntry entry)
    {
        SelectedProjectId = entry.Id;
        SearchText = entry.DisplayName;
        IsPopupOpen = false;
        RaiseEvent(new RoutedEventArgs(ProjectSelectedEvent, this));
    }
}
