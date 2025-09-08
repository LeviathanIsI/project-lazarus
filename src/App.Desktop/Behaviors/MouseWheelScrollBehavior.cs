using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lazarus.Desktop.Behaviors;

/// <summary>
/// Ensures views with a ScrollViewer respond to the mouse wheel,
/// even when focus is inside child controls or nested scrollers.
/// </summary>
public static class MouseWheelScrollBehavior
{
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(MouseWheelScrollBehavior),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement el)
        {
            if (Equals(e.NewValue, true))
            {
                el.PreviewMouseWheel += OnPreviewMouseWheel;
            }
            else
            {
                el.PreviewMouseWheel -= OnPreviewMouseWheel;
            }
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled)
            return;

        // Prefer the ScrollViewer near the mouse position
        var sv = sender as ScrollViewer ?? FindAncestor<ScrollViewer>(sender as DependencyObject);
        if (sv == null)
            return;

        // If this scroller cannot scroll, try a parent scroller
        if (sv.ScrollableHeight <= 0)
        {
            var parent = FindAncestor<ScrollViewer>(VisualTreeHelper.GetParent(sv));
            if (parent != null)
            {
                sv = parent;
            }
        }

        // Scroll by a reasonable amount based on wheel delta
        var delta = e.Delta; // 120 per notch
        var lineFactor = Math.Max(1, SystemParameters.WheelScrollLines);
        var offset = sv.VerticalOffset - (delta / 120.0) * (16 * lineFactor);
        sv.ScrollToVerticalOffset(Math.Max(0, Math.Min(sv.ScrollableHeight, offset)));
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}

