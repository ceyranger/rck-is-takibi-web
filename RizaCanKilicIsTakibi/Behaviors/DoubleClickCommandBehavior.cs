using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class DoubleClickCommandBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(DoubleClickCommandBehavior),
            new PropertyMetadata(null, OnBehaviorChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(DoubleClickCommandBehavior),
            new PropertyMetadata(null));

    public static void SetCommand(DependencyObject element, ICommand? value)
        => element.SetValue(CommandProperty, value);

    public static ICommand? GetCommand(DependencyObject element)
        => (ICommand?)element.GetValue(CommandProperty);

    public static void SetCommandParameter(DependencyObject element, object? value)
        => element.SetValue(CommandParameterProperty, value);

    public static object? GetCommandParameter(DependencyObject element)
        => element.GetValue(CommandParameterProperty);

    private static void OnBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
        if (e.NewValue is ICommand)
        {
            element.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
        }
    }

    private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not DependencyObject element)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source &&
            (FindAncestor<TextBoxBase>(source) is not null ||
             FindAncestor<ComboBox>(source) is not null))
        {
            return;
        }

        var command = GetCommand(element);
        var parameter = GetCommandParameter(element);
        if (command is null || !command.CanExecute(parameter))
        {
            return;
        }

        command.Execute(parameter);
        e.Handled = true;
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
