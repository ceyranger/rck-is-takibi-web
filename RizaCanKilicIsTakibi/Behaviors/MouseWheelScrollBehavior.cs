using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class MouseWheelScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(MouseWheelScrollBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty TargetScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "TargetScrollViewer",
            typeof(ScrollViewer),
            typeof(MouseWheelScrollBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TargetElementNameProperty =
        DependencyProperty.RegisterAttached(
            "TargetElementName",
            typeof(string),
            typeof(MouseWheelScrollBehavior),
            new PropertyMetadata(string.Empty));

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    public static void SetTargetScrollViewer(DependencyObject element, ScrollViewer? value)
        => element.SetValue(TargetScrollViewerProperty, value);

    public static ScrollViewer? GetTargetScrollViewer(DependencyObject element)
        => (ScrollViewer?)element.GetValue(TargetScrollViewerProperty);

    public static void SetTargetElementName(DependencyObject element, string? value)
        => element.SetValue(TargetElementNameProperty, value ?? string.Empty);

    public static string GetTargetElementName(DependencyObject element)
        => (string)element.GetValue(TargetElementNameProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.PreviewMouseWheel -= OnPreviewMouseWheel;
        if (e.NewValue is true)
        {
            element.PreviewMouseWheel += OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject element)
        {
            return;
        }

        var scrollViewer = ResolveTargetScrollViewer(element);
        if (scrollViewer is null || double.IsNaN(scrollViewer.ScrollableHeight) || scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var wheelLines = SystemParameters.WheelScrollLines > 0 ? SystemParameters.WheelScrollLines : 3;
        const double pixelPerLine = 16d;
        var wheelDeltaFactor = Math.Abs(e.Delta) / (double)Mouse.MouseWheelDeltaForOneLine;
        if (wheelDeltaFactor <= 0)
        {
            return;
        }

        var pixelsToScroll = wheelDeltaFactor * wheelLines * pixelPerLine;
        var targetOffset = e.Delta > 0
            ? scrollViewer.VerticalOffset - pixelsToScroll
            : scrollViewer.VerticalOffset + pixelsToScroll;
        targetOffset = targetOffset < 0 ? 0 : targetOffset;
        if (targetOffset > scrollViewer.ScrollableHeight)
        {
            targetOffset = scrollViewer.ScrollableHeight;
        }

        scrollViewer.ScrollToVerticalOffset(targetOffset);
        e.Handled = true;
    }

    private static ScrollViewer? ResolveTargetScrollViewer(DependencyObject element)
    {
        if (GetTargetScrollViewer(element) is ScrollViewer explicitScrollViewer)
        {
            return explicitScrollViewer;
        }

        var targetElementName = GetTargetElementName(element);
        if (!string.IsNullOrWhiteSpace(targetElementName))
        {
            var targetElement = FindNamedElement(element, targetElementName);
            var targetScrollViewer = ResolveScrollViewer(targetElement);
            if (targetScrollViewer is not null)
            {
                return targetScrollViewer;
            }
        }

        return ResolveScrollViewer(element);
    }

    private static ScrollViewer? ResolveScrollViewer(DependencyObject? element)
    {
        if (element is null)
        {
            return null;
        }

        if (element is ScrollViewer selfScrollViewer && selfScrollViewer.ScrollableHeight > 0)
        {
            return selfScrollViewer;
        }

        var ancestor = FindAncestor<ScrollViewer>(element);
        if (ancestor is not null && ancestor.ScrollableHeight > 0)
        {
            return ancestor;
        }

        return FindScrollableDescendant(element);
    }

    private static FrameworkElement? FindNamedElement(DependencyObject element, string targetElementName)
    {
        if (element is FrameworkElement currentElement)
        {
            var named = currentElement.FindName(targetElementName) as FrameworkElement;
            if (named is not null)
            {
                return named;
            }
        }

        var ancestor = element;
        while (ancestor is not null)
        {
            if (ancestor is FrameworkElement frameworkElement)
            {
                var named = frameworkElement.FindName(targetElementName) as FrameworkElement;
                if (named is not null)
                {
                    return named;
                }
            }

            ancestor = VisualTreeHelper.GetParent(ancestor);
        }

        return null;
    }

    private static ScrollViewer? FindScrollableDescendant(DependencyObject? source)
    {
        if (source is null)
        {
            return null;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(source);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(source, index);
            if (child is ScrollViewer scrollViewer && scrollViewer.ScrollableHeight > 0)
            {
                return scrollViewer;
            }

            var nested = FindScrollableDescendant(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            source = VisualTreeHelper.GetParent(source);
            if (source is T typed)
            {
                return typed;
            }
        }

        return null;
    }
}
