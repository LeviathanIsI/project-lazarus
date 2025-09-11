using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Lazarus.Desktop.Behaviors;

/// <summary>
/// Makes a GridViewColumn fill the remaining width of a ListView.
/// Usage:
///   - Attach EnableColumnFill="True" to the ListView
///   - Mark one GridViewColumn with FillColumn="True"
/// The column will resize on Loaded and whenever the control size changes.
/// </summary>
public static class GridViewColumnResizeBehavior
{
    public static readonly DependencyProperty EnableColumnFillProperty =
        DependencyProperty.RegisterAttached(
            "EnableColumnFill",
            typeof(bool),
            typeof(GridViewColumnResizeBehavior),
            new PropertyMetadata(false, OnEnableColumnFillChanged));

    public static void SetEnableColumnFill(DependencyObject element, bool value) => element.SetValue(EnableColumnFillProperty, value);
    public static bool GetEnableColumnFill(DependencyObject element) => (bool)element.GetValue(EnableColumnFillProperty);

    public static readonly DependencyProperty FillColumnProperty =
        DependencyProperty.RegisterAttached(
            "FillColumn",
            typeof(bool),
            typeof(GridViewColumnResizeBehavior),
            new PropertyMetadata(false, OnFillColumnChanged));

    public static void SetFillColumn(DependencyObject element, bool value) => element.SetValue(FillColumnProperty, value);
    public static bool GetFillColumn(DependencyObject element) => (bool)element.GetValue(FillColumnProperty);

    private static void OnEnableColumnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView lv)
            return;

        if (Equals(e.NewValue, true))
        {
            lv.Loaded += OnListViewLoaded;
            lv.SizeChanged += OnListViewSizeChanged;
        }
        else
        {
            lv.Loaded -= OnListViewLoaded;
            lv.SizeChanged -= OnListViewSizeChanged;
        }
    }

    private static void OnFillColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // No-op: the ListView handlers will compute widths
    }

    private static void OnListViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView lv)
        {
            HookColumns(lv);
            UpdateFillColumnWidth(lv);
        }
    }

    private static void OnListViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is ListView lv)
        {
            UpdateFillColumnWidth(lv);
        }
    }

    private static void HookColumns(ListView lv)
    {
        if (lv.View is GridView gv && gv.Columns is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += (_, __) => UpdateFillColumnWidth(lv);
        }
    }

    private static void UpdateFillColumnWidth(ListView lv)
    {
        if (lv.View is not GridView gv || gv.Columns.Count == 0)
            return;

        var fillCol = gv.Columns.FirstOrDefault(c => GetFillColumn(c));
        if (fillCol == null)
            return;

        // Estimate chrome padding and scrollbar
        const double chrome = 24; // padding/margins
        double scrollBar = SystemParameters.VerticalScrollBarWidth;
        double total = lv.ActualWidth - chrome - scrollBar;
        if (double.IsNaN(total) || total <= 0) return;

        double fixedSum = gv.Columns.Where(c => c != fillCol).Sum(c => c.Width > 0 ? c.Width : 80);
        double target = Math.Max(80, total - fixedSum);
        // Avoid thrash: only set when different enough
        if (Math.Abs(fillCol.Width - target) > 1)
        {
            fillCol.Width = target;
        }
    }
}

