using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class TextChangedCommandBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(TextChangedCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(TextChangedCommandBehavior),
            new PropertyMetadata(null));

    public static void SetCommand(DependencyObject element, ICommand? value)
        => element.SetValue(CommandProperty, value);

    public static ICommand? GetCommand(DependencyObject element)
        => (ICommand?)element.GetValue(CommandProperty);

    public static void SetCommandParameter(DependencyObject element, object? value)
        => element.SetValue(CommandParameterProperty, value);

    public static object? GetCommandParameter(DependencyObject element)
        => element.GetValue(CommandParameterProperty);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        textBox.TextChanged -= OnTextChanged;
        if (e.NewValue is ICommand)
        {
            textBox.TextChanged += OnTextChanged;
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var command = GetCommand(textBox);
        var parameter = GetCommandParameter(textBox);
        if (command is not null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }
}
