using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class TextBoxMouseWheelBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TextBoxMouseWheelBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBox textBox)
        {
            return;
        }

        textBox.PreviewMouseWheel -= OnPreviewMouseWheel;
        if (args.NewValue is true)
        {
            textBox.PreviewMouseWheel += OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
    {
        if (sender is not TextBox textBox || !textBox.AcceptsReturn)
        {
            return;
        }

        var scrollViewer = FindDescendantScrollViewer(textBox);
        if (scrollViewer is null || scrollViewer.ScrollableHeight <= 0 || double.IsNaN(scrollViewer.ScrollableHeight))
        {
            return;
        }

        var lines = SystemParameters.WheelScrollLines > 0 ? SystemParameters.WheelScrollLines : 3;
        const double pixelsPerLine = 16d;
        var wheelFactor = Math.Abs(args.Delta) / (double)Mouse.MouseWheelDeltaForOneLine;
        if (wheelFactor <= 0)
        {
            return;
        }

        var delta = wheelFactor * lines * pixelsPerLine;
        var currentOffset = scrollViewer.VerticalOffset;
        var targetOffset = args.Delta > 0
            ? currentOffset - delta
            : currentOffset + delta;

        if (targetOffset < 0)
        {
            targetOffset = 0;
        }
        else if (targetOffset > scrollViewer.ScrollableHeight)
        {
            targetOffset = scrollViewer.ScrollableHeight;
        }

        // Parent scroll should continue naturally when textbox reaches the edge.
        if (Math.Abs(targetOffset - currentOffset) < 0.1d)
        {
            return;
        }

        scrollViewer.ScrollToVerticalOffset(targetOffset);
        args.Handled = true;
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject source)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(source);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(source, index);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            var nested = FindDescendantScrollViewer(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
