using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Windows;

namespace Lazarus.App.Desktop.Collections;

/// <summary>
/// Thread-safe ObservableCollection that marshalls all collection changes to the UI thread
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
{
    private readonly Dispatcher _dispatcher;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the ThreadSafeObservableCollection class
    /// </summary>
    public ThreadSafeObservableCollection()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// Initializes a new instance with an existing collection
    /// </summary>
    /// <param name="collection">The collection to copy from</param>
    public ThreadSafeObservableCollection(IEnumerable<T> collection) : this()
    {
        if (collection != null)
        {
            foreach (var item in collection)
            {
                Add(item);
            }
        }
    }

    /// <summary>
    /// Adds an item to the collection in a thread-safe manner
    /// </summary>
    /// <param name="item">The item to add</param>
    public new void Add(T item)
    {
        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                base.Add(item);
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    base.Add(item);
                }
            });
        }
    }

    /// <summary>
    /// Removes an item from the collection in a thread-safe manner
    /// </summary>
    /// <param name="item">The item to remove</param>
    /// <returns>True if the item was removed; otherwise, false</returns>
    public new bool Remove(T item)
    {
        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                return base.Remove(item);
            }
        }
        else
        {
            return _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    return base.Remove(item);
                }
            });
        }
    }

    /// <summary>
    /// Removes the item at the specified index in a thread-safe manner
    /// </summary>
    /// <param name="index">The index of the item to remove</param>
    public new void RemoveAt(int index)
    {
        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                if (index >= 0 && index < Count)
                {
                    base.RemoveAt(index);
                }
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (index >= 0 && index < Count)
                    {
                        base.RemoveAt(index);
                    }
                }
            });
        }
    }

    /// <summary>
    /// Inserts an item at the specified index in a thread-safe manner
    /// </summary>
    /// <param name="index">The index to insert at</param>
    /// <param name="item">The item to insert</param>
    public new void Insert(int index, T item)
    {
        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                base.Insert(index, item);
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    base.Insert(index, item);
                }
            });
        }
    }

    /// <summary>
    /// Clears all items from the collection in a thread-safe manner
    /// </summary>
    public new void Clear()
    {
        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                base.Clear();
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    base.Clear();
                }
            });
        }
    }

    /// <summary>
    /// Moves an item within the collection in a thread-safe manner
    /// </summary>
    /// <param name="oldIndex">The current index of the item</param>
    /// <param name="newIndex">The target index for the item</param>
    public new void Move(int oldIndex, int newIndex)
    {
        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                if (oldIndex >= 0 && oldIndex < Count && newIndex >= 0 && newIndex < Count)
                {
                    base.Move(oldIndex, newIndex);
                }
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (oldIndex >= 0 && oldIndex < Count && newIndex >= 0 && newIndex < Count)
                    {
                        base.Move(oldIndex, newIndex);
                    }
                }
            });
        }
    }

    /// <summary>
    /// Adds a range of items to the collection efficiently
    /// </summary>
    /// <param name="items">The items to add</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;

        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                foreach (var item in items)
                {
                    base.Add(item);
                }
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    foreach (var item in items)
                    {
                        base.Add(item);
                    }
                }
            });
        }
    }

    /// <summary>
    /// Replaces all items in the collection with new items
    /// </summary>
    /// <param name="items">The new items</param>
    public void ReplaceAll(IEnumerable<T> items)
    {
        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                base.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        base.Add(item);
                    }
                }
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    base.Clear();
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            base.Add(item);
                        }
                    }
                }
            });
        }
    }

    /// <summary>
    /// Executes an action on all items safely
    /// </summary>
    /// <param name="action">The action to execute</param>
    public void ExecuteOnItems(Action<T> action)
    {
        if (action == null) return;

        if (_dispatcher.CheckAccess())
        {
            lock (_lock)
            {
                foreach (var item in this.ToList()) // Create snapshot to avoid modification during iteration
                {
                    action(item);
                }
            }
        }
        else
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    foreach (var item in this.ToList())
                    {
                        action(item);
                    }
                }
            });
        }
    }
}