using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class InlineEditActivatorBehavior
{
    public static readonly DependencyProperty BeginEditCommandProperty =
        DependencyProperty.RegisterAttached(
            "BeginEditCommand",
            typeof(ICommand),
            typeof(InlineEditActivatorBehavior),
            new PropertyMetadata(null, OnBehaviorChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(InlineEditActivatorBehavior),
            new PropertyMetadata(null));

    public static void SetBeginEditCommand(DependencyObject element, ICommand? value)
        => element.SetValue(BeginEditCommandProperty, value);

    public static ICommand? GetBeginEditCommand(DependencyObject element)
        => (ICommand?)element.GetValue(BeginEditCommandProperty);

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

        element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        element.PreviewKeyDown -= OnPreviewKeyDown;
        if (e.NewValue is ICommand)
        {
            element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            element.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not DependencyObject element)
        {
            return;
        }

        var command = GetBeginEditCommand(element);
        var parameter = GetCommandParameter(element);
        if ((parameter is null || ReferenceEquals(parameter, DependencyProperty.UnsetValue)) && element is FrameworkElement frameworkElement)
        {
            parameter = frameworkElement.DataContext;
        }

        if (command is null || !command.CanExecute(parameter))
        {
            return;
        }

        command.Execute(parameter);
        e.Handled = true;
        FocusEditor(element);
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F2 || sender is not DependencyObject element)
        {
            return;
        }

        var command = GetBeginEditCommand(element);
        var parameter = GetCommandParameter(element);
        if ((parameter is null || ReferenceEquals(parameter, DependencyProperty.UnsetValue)) && element is FrameworkElement frameworkElement)
        {
            parameter = frameworkElement.DataContext;
        }

        if (command is null || !command.CanExecute(parameter))
        {
            return;
        }

        command.Execute(parameter);
        e.Handled = true;
        FocusEditor(element);
    }

    private static void FocusEditor(DependencyObject element)
    {
        if (element is not UIElement uiElement)
        {
            return;
        }

        uiElement.Dispatcher.BeginInvoke(() =>
        {
            var focusTarget = FindDescendant<TextBox>(element);
            if (focusTarget is null)
            {
                return;
            }

            if (!focusTarget.IsVisible || !focusTarget.IsEnabled)
            {
                return;
            }

            focusTarget.Focus();
            Keyboard.Focus(focusTarget);
            focusTarget.SelectAll();
            focusTarget.CaretIndex = focusTarget.Text?.Length ?? 0;
        }, DispatcherPriority.ContextIdle);
    }

    private static T? FindDescendant<T>(DependencyObject? source) where T : DependencyObject
    {
        if (source is null)
        {
            return null;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(source);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(source, index);
            if (child is T typed)
            {
                return typed;
            }

            var nested = FindDescendant<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
