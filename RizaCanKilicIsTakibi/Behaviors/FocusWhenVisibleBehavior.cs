using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class FocusWhenVisibleBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(FocusWhenVisibleBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        textBox.Loaded -= OnTextBoxStateChanged;
        textBox.IsVisibleChanged -= OnIsVisibleChanged;

        if (e.NewValue is true)
        {
            textBox.Loaded += OnTextBoxStateChanged;
            textBox.IsVisibleChanged += OnIsVisibleChanged;
            FocusEditor(textBox);
        }
    }

    private static void OnTextBoxStateChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FocusEditor(textBox);
        }
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox textBox && e.NewValue is true)
        {
            FocusEditor(textBox);
        }
    }

    private static void FocusEditor(TextBox textBox)
    {
        if (!GetIsEnabled(textBox) || !textBox.IsVisible || !textBox.IsEnabled)
        {
            return;
        }

        textBox.Dispatcher.BeginInvoke(() =>
        {
            if (!GetIsEnabled(textBox) || !textBox.IsVisible || !textBox.IsEnabled)
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
