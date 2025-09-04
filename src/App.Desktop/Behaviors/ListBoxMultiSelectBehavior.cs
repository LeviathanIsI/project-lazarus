using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Lazarus.App.Desktop.Behaviors;

/// <summary>
/// Attached behavior to support two-way binding of ListBox.SelectedItems
/// </summary>
public static class ListBoxMultiSelectBehavior
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(ListBoxMultiSelectBehavior),
            new PropertyMetadata(null, OnSelectedItemsChanged));

    public static IList GetSelectedItems(DependencyObject obj)
    {
        return (IList)obj.GetValue(SelectedItemsProperty);
    }

    public static void SetSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(SelectedItemsProperty, value);
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
            return;

        // Remove existing handlers to prevent memory leaks
        listBox.SelectionChanged -= OnListBoxSelectionChanged;
        
        var oldCollection = e.OldValue as INotifyCollectionChanged;
        if (oldCollection != null)
        {
            oldCollection.CollectionChanged -= OnBoundCollectionChanged;
        }

        var newCollection = e.NewValue as INotifyCollectionChanged;
        if (newCollection != null)
        {
            // Handle changes in the bound collection
            newCollection.CollectionChanged += OnBoundCollectionChanged;
            
            // Sync initial state from bound collection to ListBox
            SyncFromBoundCollection(listBox, (IList)e.NewValue);
        }

        // Handle changes in ListBox selection
        listBox.SelectionChanged += OnListBoxSelectionChanged;
    }

    private static void OnBoundCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not IList boundCollection)
            return;

        // Find the ListBox associated with this bound collection
        var listBox = FindListBoxForCollection(boundCollection);
        if (listBox != null)
        {
            SyncFromBoundCollection(listBox, boundCollection);
        }
    }

    private static ListBox? FindListBoxForCollection(IList collection)
    {
        // This is a simplified approach - in a real scenario you might need a more robust way
        // to map collections back to their associated ListBox controls
        if (Application.Current?.MainWindow != null)
        {
            return FindListBoxInVisualTree(Application.Current.MainWindow, collection);
        }
        return null;
    }

    private static ListBox? FindListBoxInVisualTree(DependencyObject parent, IList collection)
    {
        if (parent is ListBox listBox && ReferenceEquals(GetSelectedItems(listBox), collection))
        {
            return listBox;
        }

        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            var result = FindListBoxInVisualTree(child, collection);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox)
            return;

        var boundCollection = GetSelectedItems(listBox) as IList;
        if (boundCollection == null)
            return;

        // Sync from ListBox selection to bound collection
        SyncToBoundCollection(listBox, boundCollection);
    }

    private static void SyncFromBoundCollection(ListBox listBox, IList boundCollection)
    {
        listBox.SelectionChanged -= OnListBoxSelectionChanged;
        
        try
        {
            listBox.SelectedItems.Clear();
            
            foreach (var item in boundCollection)
            {
                if (listBox.Items.Contains(item))
                {
                    listBox.SelectedItems.Add(item);
                }
            }
        }
        finally
        {
            listBox.SelectionChanged += OnListBoxSelectionChanged;
        }
    }

    private static void SyncToBoundCollection(ListBox listBox, IList boundCollection)
    {
        // Temporarily unhook collection change handler to prevent recursion
        if (boundCollection is INotifyCollectionChanged notifyCollection)
        {
            notifyCollection.CollectionChanged -= OnBoundCollectionChanged;
        }

        try
        {
            boundCollection.Clear();
            
            foreach (var item in listBox.SelectedItems)
            {
                boundCollection.Add(item);
            }
        }
        finally
        {
            // Re-hook collection change handler
            if (boundCollection is INotifyCollectionChanged notifyCollection2)
            {
                notifyCollection2.CollectionChanged += OnBoundCollectionChanged;
            }
        }
    }
}