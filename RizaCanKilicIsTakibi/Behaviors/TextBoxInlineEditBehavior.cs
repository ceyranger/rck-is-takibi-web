using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class TextBoxInlineEditBehavior
{
    public static readonly DependencyProperty SelectCommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "SelectCommandParameter",
            typeof(object),
            typeof(TextBoxInlineEditBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.RegisterAttached(
            "SelectCommand",
            typeof(ICommand),
            typeof(TextBoxInlineEditBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty BeginEditCommandProperty =
        DependencyProperty.RegisterAttached(
            "BeginEditCommand",
            typeof(ICommand),
            typeof(TextBoxInlineEditBehavior),
            new PropertyMetadata(null, OnBehaviorChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(TextBoxInlineEditBehavior),
            new PropertyMetadata(null));

    public static void SetBeginEditCommand(DependencyObject element, ICommand? value)
        => element.SetValue(BeginEditCommandProperty, value);

    public static ICommand? GetBeginEditCommand(DependencyObject element)
        => (ICommand?)element.GetValue(BeginEditCommandProperty);

    public static void SetSelectCommand(DependencyObject element, ICommand? value)
        => element.SetValue(SelectCommandProperty, value);

    public static ICommand? GetSelectCommand(DependencyObject element)
        => (ICommand?)element.GetValue(SelectCommandProperty);

    public static void SetSelectCommandParameter(DependencyObject element, object? value)
        => element.SetValue(SelectCommandParameterProperty, value);

    public static object? GetSelectCommandParameter(DependencyObject element)
        => element.GetValue(SelectCommandParameterProperty);

    public static void SetCommandParameter(DependencyObject element, object? value)
        => element.SetValue(CommandParameterProperty, value);

    public static object? GetCommandParameter(DependencyObject element)
        => element.GetValue(CommandParameterProperty);

    private static void OnBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        textBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        if (e.NewValue is ICommand)
        {
            textBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var parameter = GetCommandParameter(textBox);
        var selectParameter = GetSelectCommandParameter(textBox) ?? parameter;
        var selectCommand = GetSelectCommand(textBox);
        if (selectCommand is not null && selectCommand.CanExecute(selectParameter))
        {
            selectCommand.Execute(selectParameter);
        }

        if (e.ClickCount != 2)
        {
            return;
        }

        var command = GetBeginEditCommand(textBox);
        if (command is null || !command.CanExecute(parameter))
        {
            return;
        }

        command.Execute(parameter);
        e.Handled = true;

        textBox.Dispatcher.BeginInvoke(() =>
        {
            if (!textBox.IsVisible || !textBox.IsEnabled)
            {
                return;
            }

            textBox.Focus();
            Keyboard.Focus(textBox);
            textBox.SelectAll();
            textBox.CaretIndex = textBox.Text?.Length ?? 0;
        }, DispatcherPriority.ContextIdle);
    }
}
