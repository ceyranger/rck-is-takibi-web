using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class SynchronizedVerticalScrollBehavior
{
    public static readonly DependencyProperty TargetScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "TargetScrollViewer",
            typeof(ScrollViewer),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(null, OnTargetScrollViewerChanged));

    public static readonly DependencyProperty TargetElementNameProperty =
        DependencyProperty.RegisterAttached(
            "TargetElementName",
            typeof(string),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(string.Empty, OnTargetScrollViewerChanged));

    private static readonly DependencyProperty SourceScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "SourceScrollViewer",
            typeof(ScrollViewer),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(null));

    private static readonly DependencyProperty ResolvedTargetScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "ResolvedTargetScrollViewer",
            typeof(ScrollViewer),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(null));

    private static readonly DependencyProperty AttachedTargetScrollViewerProperty =
        DependencyProperty.RegisterAttached(
            "AttachedTargetScrollViewer",
            typeof(ScrollViewer),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(null));

    private static readonly DependencyProperty IsSyncingProperty =
        DependencyProperty.RegisterAttached(
            "IsSyncing",
            typeof(bool),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(false));

    private static readonly DependencyProperty RegisteredSourceScrollViewersProperty =
        DependencyProperty.RegisterAttached(
            "RegisteredSourceScrollViewers",
            typeof(List<ScrollViewer>),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(null));

    private static readonly DependencyProperty IsTargetHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsTargetHooked",
            typeof(bool),
            typeof(SynchronizedVerticalScrollBehavior),
            new PropertyMetadata(false));

    public static void SetTargetScrollViewer(DependencyObject element, ScrollViewer? value)
        => element.SetValue(TargetScrollViewerProperty, value);

    public static ScrollViewer? GetTargetScrollViewer(DependencyObject element)
        => (ScrollViewer?)element.GetValue(TargetScrollViewerProperty);

    public static void SetTargetElementName(DependencyObject element, string? value)
        => element.SetValue(TargetElementNameProperty, value ?? string.Empty);

    public static string GetTargetElementName(DependencyObject element)
        => (string)element.GetValue(TargetElementNameProperty);

    private static void SetSourceScrollViewer(DependencyObject element, ScrollViewer? value)
        => element.SetValue(SourceScrollViewerProperty, value);

    private static ScrollViewer? GetSourceScrollViewer(DependencyObject element)
        => (ScrollViewer?)element.GetValue(SourceScrollViewerProperty);

    private static void SetResolvedTargetScrollViewer(DependencyObject element, ScrollViewer? value)
        => element.SetValue(ResolvedTargetScrollViewerProperty, value);

    private static ScrollViewer? GetResolvedTargetScrollViewer(DependencyObject element)
        => (ScrollViewer?)element.GetValue(ResolvedTargetScrollViewerProperty);

    private static void SetAttachedTargetScrollViewer(DependencyObject element, ScrollViewer? value)
        => element.SetValue(AttachedTargetScrollViewerProperty, value);

    private static ScrollViewer? GetAttachedTargetScrollViewer(DependencyObject element)
        => (ScrollViewer?)element.GetValue(AttachedTargetScrollViewerProperty);

    private static void SetIsSyncing(DependencyObject element, bool value)
        => element.SetValue(IsSyncingProperty, value);

    private static bool GetIsSyncing(DependencyObject element)
        => (bool)element.GetValue(IsSyncingProperty);

    private static List<ScrollViewer> GetRegisteredSourceScrollViewers(DependencyObject element)
    {
        var viewers = (List<ScrollViewer>?)element.GetValue(RegisteredSourceScrollViewersProperty);
        if (viewers is not null)
        {
            return viewers;
        }

        viewers = [];
        element.SetValue(RegisteredSourceScrollViewersProperty, viewers);
        return viewers;
    }

    private static void SetIsTargetHooked(DependencyObject element, bool value)
        => element.SetValue(IsTargetHookedProperty, value);

    private static bool GetIsTargetHooked(DependencyObject element)
        => (bool)element.GetValue(IsTargetHookedProperty);

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

        if (e.NewValue is not null || !string.IsNullOrWhiteSpace(GetTargetElementName(element)))
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

        var target = ResolveTargetScrollViewer(element);
        if (target is null)
        {
            return;
        }

        var source = element as ScrollViewer ?? FindDescendantScrollViewer(element);
        if (source is null)
        {
            return;
        }

        SetSourceScrollViewer(element, source);
        SetResolvedTargetScrollViewer(source, target);
        SetAttachedTargetScrollViewer(element, target);
        RegisterSourceWithTarget(target, source);
    }

    private static void UnhookSourceScrollViewer(FrameworkElement element)
    {
        var source = GetSourceScrollViewer(element);
        var target = GetAttachedTargetScrollViewer(element);

        if (source is not null)
        {
            SetResolvedTargetScrollViewer(source, null);
            SetSourceScrollViewer(element, null);
        }

        if (target is not null && source is not null)
        {
            UnregisterSourceFromTarget(target, source);
        }

        SetAttachedTargetScrollViewer(element, null);
    }

    private static void OnTargetScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer target || e.VerticalChange == 0)
        {
            return;
        }

        if (GetIsSyncing(target))
        {
            return;
        }

        foreach (var source in GetRegisteredSourceScrollViewers(target).ToArray())
        {
            if (ReferenceEquals(source, target))
            {
                continue;
            }

            var sourceOffset = TranslateVerticalOffset(target, source);
            if (Math.Abs(source.VerticalOffset - sourceOffset) < 0.1d)
            {
                continue;
            }

            SetIsSyncing(source, true);
            try
            {
                source.ScrollToVerticalOffset(sourceOffset);
            }
            finally
            {
                SetIsSyncing(source, false);
            }
        }
    }

    private static double TranslateVerticalOffset(ScrollViewer from, ScrollViewer to)
    {
        if (double.IsNaN(from.VerticalOffset) || double.IsInfinity(from.VerticalOffset))
        {
            return 0d;
        }

        if (to.ScrollableHeight <= 0)
        {
            return 0d;
        }

        if (from.ScrollableHeight <= 0)
        {
            return Math.Clamp(from.VerticalOffset, 0d, to.ScrollableHeight);
        }

        var ratio = from.VerticalOffset / from.ScrollableHeight;
        if (double.IsNaN(ratio) || double.IsInfinity(ratio))
        {
            return 0d;
        }

        return Math.Clamp(ratio * to.ScrollableHeight, 0d, to.ScrollableHeight);
    }

    private static void RegisterSourceWithTarget(ScrollViewer target, ScrollViewer source)
    {
        var sources = GetRegisteredSourceScrollViewers(target);
        if (!sources.Contains(source))
        {
            sources.Add(source);
        }

        if (GetIsTargetHooked(target))
        {
            return;
        }

        target.ScrollChanged += OnTargetScrollChanged;
        SetIsTargetHooked(target, true);
    }

    private static void UnregisterSourceFromTarget(ScrollViewer target, ScrollViewer source)
    {
        var sources = GetRegisteredSourceScrollViewers(target);
        sources.Remove(source);

        if (sources.Count > 0 || !GetIsTargetHooked(target))
        {
            return;
        }

        target.ScrollChanged -= OnTargetScrollChanged;
        SetIsTargetHooked(target, false);
    }

    private static ScrollViewer? ResolveTargetScrollViewer(DependencyObject element)
    {
        if (GetTargetScrollViewer(element) is ScrollViewer explicitTarget)
        {
            return explicitTarget;
        }

        var targetElementName = GetTargetElementName(element);
        if (!string.IsNullOrWhiteSpace(targetElementName))
        {
            var targetElement = FindNamedElement(element, targetElementName);
            return targetElement is null ? null : FindDescendantScrollViewer(targetElement) ?? targetElement as ScrollViewer;
        }

        return null;
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
