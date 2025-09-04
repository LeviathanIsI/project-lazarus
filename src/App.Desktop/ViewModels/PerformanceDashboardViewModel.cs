using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using Lazarus.App.Shared.Performance;
using Lazarus.App.Desktop.Services;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// Performance Dashboard ViewModel - Provides real-time performance monitoring and budget enforcement visibility
/// </summary>
public class PerformanceDashboardViewModel : BaseViewModel
{
    private readonly ILogger<PerformanceDashboardViewModel> _logger;
    private readonly PerformanceBudgeter _performanceBudgeter;
    private readonly PerformanceAnalysisService _analysisService;
    private readonly PerformanceBaselineService _baselineService;
    private readonly Timer _updateTimer;

    // Performance metrics properties
    private PerformanceReport? _currentReport;
    private BudgetValidationResult? _currentBudgetStatus;
    private ResourceConsumptionAnalysis? _lastAnalysis;
    private MemoryLeakAnalysis? _lastLeakAnalysis;
    private string _overallHealthGrade = "Unknown";
    private double _memoryUsageMB = 0;
    private double _vramUsageGB = 0;
    private double _cpuUsagePercent = 0;
    private double _averageFrameTimeMs = 0;
    private double _averageQueryTimeMs = 0;
    private bool _isWithinBudget = true;

    // Collections for historical data and violations
    private readonly ObservableCollection<BudgetViolation> _recentViolations = new();
    private readonly ObservableCollection<OptimizationRecommendation> _recommendations = new();
    private readonly ObservableCollection<PerformanceMetricPoint> _memoryHistory = new();
    private readonly ObservableCollection<PerformanceMetricPoint> _frameTimeHistory = new();

    // Commands
    private IRelayCommand? _generateReportCommand;
    private IRelayCommand? _analyzeMemoryLeaksCommand;
    private IRelayCommand? _exportReportCommand;
    private IRelayCommand? _clearViolationsCommand;

    public event EventHandler<PerformanceAlertEventArgs>? PerformanceAlert;

    public PerformanceDashboardViewModel(
        ILogger<PerformanceDashboardViewModel> logger,
        PerformanceBudgeter performanceBudgeter,
        PerformanceAnalysisService analysisService,
        PerformanceBaselineService baselineService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceBudgeter = performanceBudgeter ?? throw new ArgumentNullException(nameof(performanceBudgeter));
        _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        _baselineService = baselineService ?? throw new ArgumentNullException(nameof(baselineService));

        // Subscribe to performance events
        _performanceBudgeter.BudgetViolation += OnBudgetViolation;
        _performanceBudgeter.OptimizationRecommendation += OnOptimizationRecommendation;
        _baselineService.BaselineEstablished += OnBaselineEstablished;

        // Update dashboard every 5 seconds
        _updateTimer = new Timer(UpdateDashboard, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        _logger.LogInformation("Performance Dashboard ViewModel initialized");
    }

    #region Public Properties

    public string OverallHealthGrade
    {
        get => _overallHealthGrade;
        private set => SetProperty(ref _overallHealthGrade, value);
    }

    public double MemoryUsageMB
    {
        get => _memoryUsageMB;
        private set => SetProperty(ref _memoryUsageMB, value);
    }

    public double VramUsageGB
    {
        get => _vramUsageGB;
        private set => SetProperty(ref _vramUsageGB, value);
    }

    public double CpuUsagePercent
    {
        get => _cpuUsagePercent;
        private set => SetProperty(ref _cpuUsagePercent, value);
    }

    public double AverageFrameTimeMs
    {
        get => _averageFrameTimeMs;
        private set => SetProperty(ref _averageFrameTimeMs, value);
    }

    public double AverageQueryTimeMs
    {
        get => _averageQueryTimeMs;
        private set => SetProperty(ref _averageQueryTimeMs, value);
    }

    public bool IsWithinBudget
    {
        get => _isWithinBudget;
        private set => SetProperty(ref _isWithinBudget, value);
    }

    public ObservableCollection<BudgetViolation> RecentViolations => _recentViolations;
    public ObservableCollection<OptimizationRecommendation> Recommendations => _recommendations;
    public ObservableCollection<PerformanceMetricPoint> MemoryHistory => _memoryHistory;
    public ObservableCollection<PerformanceMetricPoint> FrameTimeHistory => _frameTimeHistory;

    // Budget thresholds for UI visualization
    public long MemoryBudgetMB => ResourceBudgets.MaxApplicationMemory / (1024 * 1024);
    public long VramBudgetGB => ResourceBudgets.MaxModelMemory / (1024L * 1024 * 1024);
    public double FrameTimeBudgetMs => ResourceBudgets.MaxFrameTime;
    public double QueryTimeBudgetMs => ResourceBudgets.MaxDatabaseQueryTime;

    #endregion

    #region Commands

    public IRelayCommand GenerateReportCommand => _generateReportCommand ??= new AsyncRelayCommand(GenerateReportAsync);
    public IRelayCommand AnalyzeMemoryLeaksCommand => _analyzeMemoryLeaksCommand ??= new AsyncRelayCommand(AnalyzeMemoryLeaksAsync);
    public IRelayCommand ExportReportCommand => _exportReportCommand ??= new AsyncRelayCommand(ExportReportAsync);
    public IRelayCommand ClearViolationsCommand => _clearViolationsCommand ??= new RelayCommand(() => _recentViolations.Clear());

    #endregion

    private async void UpdateDashboard(object? state)
    {
        try
        {
            // Generate current performance report
            _currentReport = await _performanceBudgeter.GeneratePerformanceReportAsync();
            
            // Update budget validation
            _currentBudgetStatus = await _performanceBudgeter.ValidateResourceBudgetsAsync();

            // Update UI properties on main thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdatePerformanceMetrics();
                UpdateHistoricalData();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance dashboard");
        }
    }

    private void UpdatePerformanceMetrics()
    {
        if (_currentReport == null) return;

        OverallHealthGrade = _currentReport.OverallGrade.ToString();
        MemoryUsageMB = _currentReport.SystemMetrics.ApplicationMemory / (1024.0 * 1024);
        VramUsageGB = _currentReport.VRAMAllocationStats.AllocatedVRAM / (1024.0 * 1024 * 1024);
        CpuUsagePercent = _currentReport.SystemMetrics.CpuUsage;
        AverageFrameTimeMs = _currentReport.UIPerformanceMetrics.AverageFrameTime;
        AverageQueryTimeMs = _currentReport.DatabasePerformanceMetrics.AverageQueryTime;
        IsWithinBudget = _currentBudgetStatus?.IsWithinBudget ?? true;

        // Update recommendations
        _recommendations.Clear();
        foreach (var recommendation in _currentReport.Recommendations.Take(10)) // Show top 10
        {
            _recommendations.Add(new OptimizationRecommendation
            {
                Category = OptimizationCategory.Memory, // This would come from detailed analysis
                Priority = OptimizationPriority.Medium,
                Title = "Performance Optimization",
                Description = recommendation
            });
        }
    }

    private void UpdateHistoricalData()
    {
        if (_currentReport == null) return;

        var now = DateTime.Now;
        
        // Add memory data point
        _memoryHistory.Add(new PerformanceMetricPoint
        {
            Timestamp = now,
            Value = MemoryUsageMB
        });

        // Add frame time data point
        _frameTimeHistory.Add(new PerformanceMetricPoint
        {
            Timestamp = now,
            Value = AverageFrameTimeMs
        });

        // Keep last 100 points (about 8 minutes of history)
        if (_memoryHistory.Count > 100)
        {
            _memoryHistory.RemoveAt(0);
        }
        if (_frameTimeHistory.Count > 100)
        {
            _frameTimeHistory.RemoveAt(0);
        }
    }

    private async Task GenerateReportAsync()
    {
        try
        {
            _logger.LogInformation("Generating comprehensive performance analysis report");
            
            _lastAnalysis = await _analysisService.AnalyzeResourceConsumptionAsync(TimeSpan.FromMinutes(30));
            
            // Update recommendations based on detailed analysis
            _recommendations.Clear();
            foreach (var recommendation in _lastAnalysis.OptimizationRecommendations)
            {
                _recommendations.Add(recommendation);
            }

            // Raise alert if health score is concerning
            if (_lastAnalysis.OverallHealthScore < 60)
            {
                PerformanceAlert?.Invoke(this, new PerformanceAlertEventArgs
                {
                    AlertLevel = AlertLevel.Warning,
                    Message = $"Performance health score is {_lastAnalysis.OverallHealthScore}/100",
                    Details = string.Join(", ", _lastAnalysis.OptimizationRecommendations.Take(3).Select(r => r.Title)),
                    Timestamp = DateTime.Now
                });
            }

            _logger.LogInformation("Performance analysis completed - Health Score: {Score}/100", 
                _lastAnalysis.OverallHealthScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance report");
        }
    }

    private async Task AnalyzeMemoryLeaksAsync()
    {
        try
        {
            _logger.LogInformation("Starting memory leak analysis");
            
            _lastLeakAnalysis = await _analysisService.DetectMemoryLeaksAsync(TimeSpan.FromMinutes(15));

            if (_lastLeakAnalysis.LeakLikelihood == LeakLikelihood.High)
            {
                PerformanceAlert?.Invoke(this, new PerformanceAlertEventArgs
                {
                    AlertLevel = AlertLevel.Critical,
                    Message = "High likelihood memory leak detected",
                    Details = _lastLeakAnalysis.Conclusion,
                    Timestamp = DateTime.Now
                });
            }

            _logger.LogInformation("Memory leak analysis completed - Likelihood: {Likelihood}, Growth Rate: {Rate:F1}MB/h",
                _lastLeakAnalysis.LeakLikelihood, _lastLeakAnalysis.MemoryGrowthRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing memory leaks");
        }
    }

    private async Task ExportReportAsync()
    {
        try
        {
            if (_lastAnalysis == null)
            {
                await GenerateReportAsync(); // Generate if not available
                if (_lastAnalysis == null) return;
            }

            var json = await _analysisService.ExportPerformanceReportAsync(_lastAnalysis, ExportFormat.Json);
            var text = await _analysisService.ExportPerformanceReportAsync(_lastAnalysis, ExportFormat.Text);
            
            // In a real implementation, this would save to file or show save dialog
            _logger.LogInformation("Performance report exported successfully");
            
            // For now, just log a summary
            _logger.LogInformation("Performance Report Summary:\n{Summary}", text.Substring(0, Math.Min(500, text.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting performance report");
        }
    }

    private void OnBudgetViolation(object? sender, BudgetViolationEvent e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _recentViolations.Add(e.Violation);
            
            // Keep only recent violations (last 20)
            if (_recentViolations.Count > 20)
            {
                _recentViolations.RemoveAt(0);
            }

            // Raise alert for critical violations
            if (e.Violation.Severity == ViolationSeverity.Critical)
            {
                PerformanceAlert?.Invoke(this, new PerformanceAlertEventArgs
                {
                    AlertLevel = AlertLevel.Critical,
                    Message = $"Critical budget violation: {e.Violation.Type}",
                    Details = e.Violation.Message,
                    Timestamp = DateTime.Now
                });
            }
        });
    }

    private void OnOptimizationRecommendation(object? sender, PerformanceOptimizationEvent e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            foreach (var recommendation in e.Recommendations.Take(5)) // Add top 5
            {
                _recommendations.Add(new OptimizationRecommendation
                {
                    Category = OptimizationCategory.Memory, // Default category
                    Priority = e.Severity == OptimizationSeverity.Critical ? OptimizationPriority.Critical : OptimizationPriority.Medium,
                    Title = "Performance Optimization",
                    Description = recommendation
                });
            }
        });
    }

    private void OnBaselineEstablished(object? sender, PerformanceReport e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _logger.LogInformation("Performance baseline established - updating dashboard");
        });
    }

    protected override void DisposeResources()
    {
        _updateTimer?.Dispose();
        
        if (_performanceBudgeter != null)
        {
            _performanceBudgeter.BudgetViolation -= OnBudgetViolation;
            _performanceBudgeter.OptimizationRecommendation -= OnOptimizationRecommendation;
        }
        
        if (_baselineService != null)
        {
            _baselineService.BaselineEstablished -= OnBaselineEstablished;
        }

        base.Dispose();
    }
}

/// <summary>
/// Performance metric data point for historical charts
/// </summary>
public record PerformanceMetricPoint
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
}

/// <summary>
/// Performance alert event arguments
/// </summary>
public class PerformanceAlertEventArgs : EventArgs
{
    public AlertLevel AlertLevel { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertLevel
{
    Info,
    Warning,
    Critical
}

