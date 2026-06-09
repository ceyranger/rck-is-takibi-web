using RizaCanKilicIsTakibi.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Threading;

namespace RizaCanKilicIsTakibi.Helpers;

internal static class PendingEditCommitHelper
{
    public static void FlushFocusedEditor()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            return;
        }

        FlushElement(Keyboard.FocusedElement as DependencyObject);
    }

    internal static void FlushElement(DependencyObject? element)
    {
        if (element is null)
        {
            return;
        }

        switch (element)
        {
            case TextBox textBox:
                UpdateBinding(textBox, TextBox.TextProperty);
                ExecuteAttachedCommitCommand(textBox);
                break;
            case ComboBox comboBox:
                UpdateBinding(comboBox, ComboBox.TextProperty);
                UpdateBinding(comboBox, Selector.SelectedItemProperty);
                UpdateBinding(comboBox, Selector.SelectedValueProperty);
                break;
            case DatePicker datePicker:
                UpdateBinding(datePicker, DatePicker.SelectedDateProperty);
                UpdateBinding(datePicker, DatePicker.TextProperty);
                break;
            case ToggleButton toggleButton:
                UpdateBinding(toggleButton, ToggleButton.IsCheckedProperty);
                break;
        }

        if (FindAncestor<DataGrid>(element) is { } dataGrid)
        {
            dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }

    private static void ExecuteAttachedCommitCommand(TextBox textBox)
    {
        var command = TextBoxEditCommitBehavior.GetCommitCommand(textBox);
        if (command is null)
        {
            return;
        }

        var parameter = TextBoxEditCommitBehavior.GetCommandParameter(textBox);
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    private static void UpdateBinding(DependencyObject target, DependencyProperty property)
        => BindingOperations.GetBindingExpression(target, property)?.UpdateSource();

    private static T? FindAncestor<T>(DependencyObject element) where T : DependencyObject
    {
        var current = element;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = current switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(current),
                _ => LogicalTreeHelper.GetParent(current)
            };
        }

        return null;
    }
}
