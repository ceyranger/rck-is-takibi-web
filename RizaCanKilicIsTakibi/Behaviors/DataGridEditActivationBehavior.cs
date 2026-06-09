using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class DataGridEditActivationBehavior
{
    private static readonly DependencyProperty IsEditArmedProperty =
        DependencyProperty.RegisterAttached(
            "IsEditArmed",
            typeof(bool),
            typeof(DataGridEditActivationBehavior),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridEditActivationBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    private static bool GetIsEditArmed(DependencyObject element)
        => (bool)element.GetValue(IsEditArmedProperty);

    private static void SetIsEditArmed(DependencyObject element, bool value)
        => element.SetValue(IsEditArmedProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        Detach(grid);

        if (e.NewValue is true)
        {
            Attach(grid);
        }
    }

    private static void Attach(DataGrid grid)
    {
        grid.Loaded += OnLoaded;
        grid.BeginningEdit += OnBeginningEdit;
        grid.CellEditEnding += OnCellEditEnding;
        grid.PreviewMouseDoubleClick += OnPreviewMouseDoubleClick;
        grid.PreviewKeyDown += OnPreviewKeyDown;

        if (grid.IsLoaded)
        {
            grid.IsReadOnly = true;
        }
    }

    private static void Detach(DataGrid grid)
    {
        grid.Loaded -= OnLoaded;
        grid.BeginningEdit -= OnBeginningEdit;
        grid.CellEditEnding -= OnCellEditEnding;
        grid.PreviewMouseDoubleClick -= OnPreviewMouseDoubleClick;
        grid.PreviewKeyDown -= OnPreviewKeyDown;
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            grid.IsReadOnly = true;
            SetIsEditArmed(grid, false);
        }
    }

    private static void OnPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
        if (cell is null)
        {
            return;
        }

        BeginEdit(grid, cell);
        e.Handled = true;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (e.Key == Key.Enter && FindAncestor<DataGridCell>(Keyboard.FocusedElement as DependencyObject) is { IsEditing: true })
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
            Keyboard.ClearFocus();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.F2 || grid.CurrentCell.Column is null)
        {
            return;
        }

        BeginEdit(grid, null);
        e.Handled = true;
    }

    private static void OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (!GetIsEditArmed(grid))
        {
            e.Cancel = true;
            return;
        }

        SetIsEditArmed(grid, false);
    }

    private static void OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (e.EditingElement is TextBox textBox)
        {
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        grid.Dispatcher.BeginInvoke(() =>
        {
            grid.IsReadOnly = true;
            SetIsEditArmed(grid, false);
        }, DispatcherPriority.Background);
    }

    private static void BeginEdit(DataGrid grid, DataGridCell? cell)
    {
        SetIsEditArmed(grid, true);
        grid.IsReadOnly = false;

        if (cell is not null)
        {
            grid.CurrentCell = new DataGridCellInfo(cell);
            cell.Focus();
        }

        grid.BeginEdit();
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
