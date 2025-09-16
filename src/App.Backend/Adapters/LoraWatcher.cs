using System.Collections.Concurrent;

namespace Lazarus.Backend.Adapters;

public sealed class LoraWatcher : IDisposable
{
    private readonly FileSystemWatcher _fsw;
    private readonly System.Timers.Timer _debounce;
    private volatile bool _dirty;

    public event EventHandler? Changed;

    public LoraWatcher()
    {
        var root = LoraScanner.GetRoot();
        Directory.CreateDirectory(root);

        _fsw = new FileSystemWatcher(root)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite
        };
        _fsw.Created += OnFired;
        _fsw.Deleted += OnFired;
        _fsw.Renamed += OnFired;
        _fsw.Changed += OnFired;

        _debounce = new System.Timers.Timer(400) { AutoReset = false };
        _debounce.Elapsed += (_, __) => { if (_dirty) Changed?.Invoke(this, EventArgs.Empty); _dirty = false; };
    }

    private void OnFired(object? s, FileSystemEventArgs e) { _dirty = true; _debounce.Stop(); _debounce.Start(); }

    public void Dispose()
    {
        _fsw.Dispose();
        _debounce.Dispose();
    }
}
