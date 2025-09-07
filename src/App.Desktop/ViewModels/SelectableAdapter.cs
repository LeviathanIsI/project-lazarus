using Lazarus.Desktop.Services;
using Lazarus.Shared;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.ViewModels;

public class SelectableAdapter : ViewModelBase
{
    public AdapterInfo Info { get; }

    private readonly IOrchestratorClient _orchestrator; // example dep
    private readonly ILogger<SelectableAdapter> _logger; // example dep

    // Keep AdapterInfo as a ctor parameter. DI will NOT resolve it — the factory will pass it.
    public SelectableAdapter(
        AdapterInfo info,
        IOrchestratorClient orchestrator,
        ILogger<SelectableAdapter> logger)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
        _orchestrator = orchestrator;
        _logger = logger;

        _logger.LogDebug("SelectableAdapter created for {Adapter}", Info.Name);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

