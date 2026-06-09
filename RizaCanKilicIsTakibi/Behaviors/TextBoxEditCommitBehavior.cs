using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class TextBoxEditCommitBehavior
{
    private static readonly DependencyProperty SuppressLostFocusCommitProperty =
        DependencyProperty.RegisterAttached(
            "SuppressLostFocusCommit",
            typeof(bool),
            typeof(TextBoxEditCommitBehavior),
            new PropertyMetadata(false));

    public static readonly DependencyProperty CommitCommandProperty =
        DependencyProperty.RegisterAttached(
            "CommitCommand",
            typeof(ICommand),
            typeof(TextBoxEditCommitBehavior),
            new PropertyMetadata(null, OnBehaviorChanged));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.RegisterAttached(
            "CancelCommand",
            typeof(ICommand),
            typeof(TextBoxEditCommitBehavior),
            new PropertyMetadata(null, OnBehaviorChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(TextBoxEditCommitBehavior),
            new PropertyMetadata(null));

    public static void SetCommitCommand(DependencyObject element, ICommand? value)
        => element.SetValue(CommitCommandProperty, value);

    public static ICommand? GetCommitCommand(DependencyObject element)
        => (ICommand?)element.GetValue(CommitCommandProperty);

    public static void SetCancelCommand(DependencyObject element, ICommand? value)
        => element.SetValue(CancelCommandProperty, value);

    public static ICommand? GetCancelCommand(DependencyObject element)
        => (ICommand?)element.GetValue(CancelCommandProperty);

    public static void SetCommandParameter(DependencyObject element, object? value)
        => element.SetValue(CommandParameterProperty, value);

    public static object? GetCommandParameter(DependencyObject element)
        => element.GetValue(CommandParameterProperty);

    private static bool GetSuppressLostFocusCommit(DependencyObject element)
        => (bool)element.GetValue(SuppressLostFocusCommitProperty);

    private static void SetSuppressLostFocusCommit(DependencyObject element, bool value)
        => element.SetValue(SuppressLostFocusCommitProperty, value);

    private static void OnBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        textBox.LostFocus -= OnLostFocus;
        textBox.PreviewKeyDown -= OnPreviewKeyDown;

        if (GetCommitCommand(textBox) is not null || GetCancelCommand(textBox) is not null)
        {
            textBox.LostFocus += OnLostFocus;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (GetSuppressLostFocusCommit(textBox))
        {
            SetSuppressLostFocusCommit(textBox, false);
            return;
        }

        Commit(textBox);
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            Commit(textBox);
            SetSuppressLostFocusCommit(textBox, true);
            Keyboard.ClearFocus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Cancel(textBox);
            SetSuppressLostFocusCommit(textBox, true);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private static void Commit(TextBox textBox)
    {
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

        var command = GetCommitCommand(textBox);
        var parameter = GetCommandParameter(textBox);
        if (command is not null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    private static void Cancel(TextBox textBox)
    {
        var command = GetCancelCommand(textBox);
        var parameter = GetCommandParameter(textBox);
        if (command is not null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }
}
