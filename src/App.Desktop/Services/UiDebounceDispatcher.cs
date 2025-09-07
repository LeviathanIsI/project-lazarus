using System;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace Lazarus.Desktop.Services;

/// <summary>
/// UI-thread debounce helper built on DispatcherTimer.
/// </summary>
public sealed class UiDebounceDispatcher
{
    private readonly ConcurrentDictionary<string, DispatcherTimer> _timers = new();

    /// <summary>
    /// Debounces the specified action by key; subsequent calls reset the delay.
    /// Action runs on the UI thread.
    /// </summary>
    public void Debounce(string key, TimeSpan delay, Action action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));

        var dispatcher = System.Windows.Application.Current?.Dispatcher
                         ?? throw new InvalidOperationException("No UI dispatcher available");

        var timer = _timers.AddOrUpdate(key,
            _ => new DispatcherTimer(DispatcherPriority.Background, dispatcher),
            (_, existing) => existing);

        timer.Stop();
        timer.Interval = delay;
        timer.Tick -= OnTick;
        timer.Tick += OnTick;

        void OnTick(object? sender, EventArgs e)
        {
            timer.Stop();
            timer.Tick -= OnTick;
            action();
        }

        timer.Start();
    }
}

