using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Lazarus.Desktop.Extensions;

public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Efficiently updates an ObservableCollection to match the provided sequence.
    /// Minimizes change notifications by replacing where possible, trimming extras,
    /// and appending new items. Order is preserved.
    /// </summary>
    public static void SmartReset<T>(this ObservableCollection<T> target, IEnumerable<T> items)
    {
        var list = items as IList<T> ?? new List<T>(items);

        int i = 0;
        // Replace existing slots
        for (; i < target.Count && i < list.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(target[i], list[i]))
            {
                target[i] = list[i];
            }
        }

        // Remove extra items
        while (target.Count > list.Count)
        {
            target.RemoveAt(target.Count - 1);
        }

        // Add missing items
        for (; i < list.Count; i++)
        {
            target.Add(list[i]);
        }
    }
}

