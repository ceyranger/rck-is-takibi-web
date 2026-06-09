using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class AutoScaleToWidthBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AutoScaleToWidthBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty MinScaleProperty =
        DependencyProperty.RegisterAttached(
            "MinScale",
            typeof(double),
            typeof(AutoScaleToWidthBehavior),
            new PropertyMetadata(0.70d, OnMinScaleChanged));

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    public static void SetMinScale(DependencyObject element, double value)
        => element.SetValue(MinScaleProperty, value);

    public static double GetMinScale(DependencyObject element)
        => (double)element.GetValue(MinScaleProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= OnLayoutRelatedEvent;
        element.SizeChanged -= OnLayoutRelatedEvent;

        if (e.NewValue is true)
        {
            element.Loaded += OnLayoutRelatedEvent;
            element.SizeChanged += OnLayoutRelatedEvent;
        }
    }

    private static void OnMinScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && GetIsEnabled(element))
        {
            UpdateScale(element);
        }
    }

    private static void OnLayoutRelatedEvent(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            UpdateScale(element);
        }
    }

    private static void OnLayoutRelatedEvent(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            UpdateScale(element);
        }
    }

    private static void UpdateScale(FrameworkElement element)
    {
        if (!GetIsEnabled(element))
        {
            return;
        }

        var minScale = GetMinScale(element);
        if (double.IsNaN(minScale) || double.IsInfinity(minScale))
        {
            minScale = 0.70d;
        }

        minScale = Math.Max(0.1d, Math.Min(1.0d, minScale));

        var availableWidth = ResolveAvailableWidth(element);
        if (availableWidth <= 0 || double.IsNaN(availableWidth))
        {
            return;
        }

        var currentScale = ResolveCurrentScale(element.LayoutTransform);
        var naturalWidth = MeasureNaturalWidth(element);
        if (naturalWidth <= 0 || double.IsNaN(naturalWidth))
        {
            return;
        }

        var targetScale = Math.Min(1.0d, availableWidth / naturalWidth);
        targetScale = Math.Max(minScale, targetScale);

        if (Math.Abs(targetScale - currentScale) < 0.001d)
        {
            return;
        }

        element.LayoutTransform = Math.Abs(targetScale - 1.0d) < 0.001d
            ? Transform.Identity
            : new ScaleTransform(targetScale, targetScale);
    }

    private static double MeasureNaturalWidth(FrameworkElement element)
    {
        var previousTransform = element.LayoutTransform;
        element.LayoutTransform = Transform.Identity;
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var naturalWidth = element.DesiredSize.Width;
        element.LayoutTransform = previousTransform;
        return naturalWidth;
    }

    private static double ResolveAvailableWidth(FrameworkElement element)
    {
        var scrollViewer = FindAncestor<ScrollViewer>(element);
        if (scrollViewer is not null)
        {
            if (scrollViewer.ViewportWidth > 0 && !double.IsNaN(scrollViewer.ViewportWidth))
            {
                return scrollViewer.ViewportWidth;
            }

            if (scrollViewer.ActualWidth > 0 && !double.IsNaN(scrollViewer.ActualWidth))
            {
                return scrollViewer.ActualWidth;
            }
        }

        if (element.Parent is FrameworkElement parent && parent.ActualWidth > 0 && !double.IsNaN(parent.ActualWidth))
        {
            return parent.ActualWidth;
        }

        return element.ActualWidth;
    }

    private static double ResolveCurrentScale(Transform? transform)
    {
        return transform is ScaleTransform scaleTransform && scaleTransform.ScaleX > 0
            ? scaleTransform.ScaleX
            : 1.0d;
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
