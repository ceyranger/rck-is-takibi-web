using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class SynchronizedHorizontalScrollBehavior
{
    public static readonly DependencyProperty TargetScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "TargetScrollViewer",
            typeof(ScrollViewer),
            typeof(SynchronizedHorizontalScrollBehavior),
            new PropertyMetadata(null, OnTargetScrollViewerChanged));

    private static readonly DependencyProperty SourceScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "SourceScrollViewer",
            typeof(ScrollViewer),
            typeof(SynchronizedHorizontalScrollBehavior),
            new PropertyMetadata(null));

    public static void SetTargetScrollViewer(DependencyObject element, ScrollViewer? value)
        => element.SetValue(TargetScrollViewerProperty, value);

    public static ScrollViewer? GetTargetScrollViewer(DependencyObject element)
        => (ScrollViewer?)element.GetValue(TargetScrollViewerProperty);

    private static void SetSourceScrollViewer(DependencyObject element, ScrollViewer? value)
        => element.SetValue(SourceScrollViewerProperty, value);

    private static ScrollViewer? GetSourceScrollViewer(DependencyObject element)
        => (ScrollViewer?)element.GetValue(SourceScrollViewerProperty);

    private static void OnTargetScrollViewerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= OnElementLoaded;
        element.Unloaded -= OnElementUnloaded;
        element.Loaded += OnElementLoaded;
        element.Unloaded += OnElementUnloaded;

        if (e.NewValue is ScrollViewer)
        {
            HookSourceScrollViewer(element);
        }
        else
        {
            UnhookSourceScrollViewer(element);
        }
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            HookSourceScrollViewer(element);
        }
    }

    private static void OnElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            UnhookSourceScrollViewer(element);
        }
    }

    private static void HookSourceScrollViewer(FrameworkElement element)
    {
        UnhookSourceScrollViewer(element);

        if (GetTargetScrollViewer(element) is null)
        {
            return;
        }

        var source = element as ScrollViewer ?? FindDescendantScrollViewer(element);
        if (source is null)
        {
            return;
        }

        source.ScrollChanged += OnSourceScrollChanged;
        SetSourceScrollViewer(element, source);
    }

    private static void UnhookSourceScrollViewer(FrameworkElement element)
    {
        var source = GetSourceScrollViewer(element);
        if (source is null)
        {
            return;
        }

        source.ScrollChanged -= OnSourceScrollChanged;
        SetSourceScrollViewer(element, null);
    }

    private static void OnSourceScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer source || e.HorizontalChange == 0)
        {
            return;
        }

        var target = GetTargetScrollViewer(source);
        if (target is null)
        {
            return;
        }

        if (!double.IsNaN(target.HorizontalOffset) && !double.IsInfinity(e.HorizontalOffset))
        {
            target.ScrollToHorizontalOffset(source.HorizontalOffset);
        }
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject? source)
    {
        if (source is null)
        {
            return null;
        }

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
