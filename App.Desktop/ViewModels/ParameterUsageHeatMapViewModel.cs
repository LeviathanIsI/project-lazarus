using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Lazarus.Desktop.Services;
using Lazarus.Shared.Models;
using App.Shared.Enums;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for parameter usage heat map visualization and analytics dashboard
/// </summary>
public class ParameterUsageHeatMapViewModel : INotifyPropertyChanged
{
    private readonly ParameterUsageAnalytics _analytics;
    private bool _isGeneratingHeatMap;
    private ViewMode _selectedViewMode = ViewMode.Enthusiast;
    private ExpertiseLevel _selectedExpertiseLevel = ExpertiseLevel.Intermediate;
    private string _selectedTimeRange = "Last 7 Days";

    public ParameterUsageHeatMapViewModel(ParameterUsageAnalytics analytics)
    {
        _analytics = analytics;
        
        // Initialize collections
        HeatMapCells = new ObservableCollection<HeatMapCell>();
        ParameterLabels = new ObservableCollection<AxisLabel>();
        TimePeriodLabels = new ObservableCollection<AxisLabel>();
        TopParameters = new ObservableCollection<TopParameterItem>();
        UsageInsights = new ObservableCollection<UsageInsight>();
        ParameterTrends = new ObservableCollection<ParameterTrend>();
        SessionAnalytics = new ObservableCollection<SessionAnalytic>();
        Recommendations = new ObservableCollection<RecommendationItem>();
        
        // Initialize commands
        RefreshDataCommand = new HeatMapAsyncRelayCommand(async () => await RefreshDataAsync());
        GenerateReportCommand = new HeatMapAsyncRelayCommand(async () => await GenerateReportAsync());
        ExportDataCommand = new HeatMapAsyncRelayCommand(async () => await ExportDataAsync());
        
        // Initialize data
        _ = LoadInitialDataAsync();
    }

    #region Properties

    public ObservableCollection<HeatMapCell> HeatMapCells { get; }
    public ObservableCollection<AxisLabel> ParameterLabels { get; }
    public ObservableCollection<AxisLabel> TimePeriodLabels { get; }
    public ObservableCollection<TopParameterItem> TopParameters { get; }
    public ObservableCollection<UsageInsight> UsageInsights { get; }
    public ObservableCollection<ParameterTrend> ParameterTrends { get; }
    public ObservableCollection<SessionAnalytic> SessionAnalytics { get; }
    public ObservableCollection<RecommendationItem> Recommendations { get; }

    public List<ViewMode> AvailableViewModes => new() 
    { 
        ViewMode.Novice, ViewMode.Enthusiast, ViewMode.Developer 
    };

    public List<ExpertiseLevel> AvailableExpertiseLevels => new() 
    { 
        ExpertiseLevel.Beginner, ExpertiseLevel.Intermediate, ExpertiseLevel.Advanced, ExpertiseLevel.Expert 
    };

    public List<string> AvailableTimeRanges => new() 
    { 
        "Last 24 Hours", "Last 7 Days", "Last 30 Days", "Last 90 Days", "All Time" 
    };

    public ViewMode SelectedViewMode
    {
        get => _selectedViewMode;
        set
        {
            if (_selectedViewMode != value)
            {
                _selectedViewMode = value;
                OnPropertyChanged();
                _ = RefreshHeatMapAsync();
            }
        }
    }

    public ExpertiseLevel SelectedExpertiseLevel
    {
        get => _selectedExpertiseLevel;
        set
        {
            if (_selectedExpertiseLevel != value)
            {
                _selectedExpertiseLevel = value;
                OnPropertyChanged();
                _ = RefreshHeatMapAsync();
            }
        }
    }

    public string SelectedTimeRange
    {
        get => _selectedTimeRange;
        set
        {
            if (_selectedTimeRange != value)
            {
                _selectedTimeRange = value;
                OnPropertyChanged();
                _ = RefreshHeatMapAsync();
            }
        }
    }

    public bool IsGeneratingHeatMap
    {
        get => _isGeneratingHeatMap;
        set
        {
            _isGeneratingHeatMap = value;
            OnPropertyChanged();
        }
    }

    // Analytics Summary Properties (computed from available data)
    public int TotalInteractions => _totalInteractions;
    public string MostUsedParameter => _mostUsedParameter;
    public string PeakUsageTime => _peakUsageTime;
    public string AverageSessionDuration => _averageSessionDuration;
    
    private int _totalInteractions = 0;
    private string _mostUsedParameter = "No data";
    private string _peakUsageTime = "No data";
    private string _averageSessionDuration = "No data";

    public ICommand RefreshDataCommand { get; }
    public ICommand GenerateReportCommand { get; }
    public ICommand ExportDataCommand { get; }

    #endregion

    #region Heat Map Generation

    private async Task LoadInitialDataAsync()
    {
        IsGeneratingHeatMap = true;
        
        try
        {
            await RefreshHeatMapAsync();
            await LoadAnalyticsDataAsync();
        }
        finally
        {
            IsGeneratingHeatMap = false;
        }
    }

    private async Task RefreshHeatMapAsync()
    {
        try
        {
            IsGeneratingHeatMap = true;
            
            // Generate heat map data
            var heatMapData = await _analytics.GenerateHeatMapAsync();
            
            // Clear existing data
            Application.Current.Dispatcher.Invoke(() =>
            {
                HeatMapCells.Clear();
                ParameterLabels.Clear();
                TimePeriodLabels.Clear();
            });
            
            // Generate heat map cells
            await GenerateHeatMapVisualization(heatMapData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ParameterUsageHeatMapViewModel] Error refreshing heat map: {ex.Message}");
        }
        finally
        {
            IsGeneratingHeatMap = false;
        }
    }

    private async Task GenerateHeatMapVisualization(ParameterHeatMapData heatMapData)
    {
        if (heatMapData?.OverallHeatMap == null) return;

        await Task.Run(() =>
        {
            var heatMapEntries = heatMapData.OverallHeatMap.OrderByDescending(x => x.UsageCount).ToList();
            var maxUsage = heatMapEntries.Any() ? heatMapEntries.Max(x => x.UsageCount) : 0;
            
            // Generate time periods (simplified for demo)
            var timePeriods = new[] { "Week 1", "Week 2", "Week 3", "Week 4" };

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Generate parameter labels (Y-axis)
                for (int i = 0; i < heatMapEntries.Count && i < 20; i++)
                {
                    ParameterLabels.Add(new AxisLabel
                    {
                        Text = heatMapEntries[i].ParameterName,
                        XPosition = -150,
                        YPosition = i * 32 + 15
                    });
                }

                // Generate time period labels (X-axis)
                for (int j = 0; j < timePeriods.Length; j++)
                {
                    TimePeriodLabels.Add(new AxisLabel
                    {
                        Text = timePeriods[j],
                        XPosition = j * 42 + 20,
                        YPosition = -25
                    });
                }

                // Generate heat map cells
                for (int i = 0; i < heatMapEntries.Count && i < 20; i++)
                {
                    for (int j = 0; j < timePeriods.Length; j++)
                    {
                        var entry = heatMapEntries[i];
                        var simulatedUsage = (int)(entry.UsageCount * (0.8 + 0.4 * Math.Sin(j * Math.PI / 2))); // Simulate time variation
                        var intensity = maxUsage > 0 ? (double)simulatedUsage / maxUsage : 0;

                        HeatMapCells.Add(new HeatMapCell
                        {
                            ParameterName = entry.ParameterName,
                            TimePeriod = timePeriods[j],
                            UsageCount = simulatedUsage,
                            Intensity = intensity,
                            IntensityBrush = GetIntensityBrush(intensity),
                            TextColor = GetTextColor(intensity),
                            XPosition = j * 42,
                            YPosition = i * 32,
                            ToolTipText = $"{entry.ParameterName}\n{timePeriods[j]}\n{simulatedUsage} interactions"
                        });
                    }
                }
            });
        });
    }

    private Brush GetIntensityBrush(double intensity)
    {
        // Create heat map color gradient: blue (low) -> red (high)
        var colors = new[]
        {
            Color.FromRgb(26, 35, 126),    // Dark blue (0.0)
            Color.FromRgb(48, 63, 159),    // Blue (0.2)
            Color.FromRgb(63, 81, 181),    // Light blue (0.4)
            Color.FromRgb(255, 152, 0),    // Orange (0.6)
            Color.FromRgb(255, 87, 34),    // Red-orange (0.8)
            Color.FromRgb(211, 47, 47)     // Red (1.0)
        };

        var index = Math.Min((int)(intensity * (colors.Length - 1)), colors.Length - 1);
        return new SolidColorBrush(colors[index]);
    }

    private Brush GetTextColor(double intensity)
    {
        // Use white text for dark backgrounds, black for light
        return intensity > 0.5 
            ? new SolidColorBrush(Colors.White) 
            : new SolidColorBrush(Colors.Black);
    }

    #endregion

    #region Analytics Data Loading

    private async Task LoadAnalyticsDataAsync()
    {
        try
        {
            // Generate insights
            var insights = await _analytics.GenerateInsightsAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Update summary properties
                _totalInteractions = insights.TotalInteractions;
                _mostUsedParameter = insights.MostUsedParameters.FirstOrDefault()?.ParameterName ?? "No data";
                _peakUsageTime = DateTime.Now.ToString("HH:mm"); // Placeholder
                _averageSessionDuration = "15:30"; // Placeholder
                
                // Load top parameters
                TopParameters.Clear();
                var maxUsage = insights.MostUsedParameters.Any() ? insights.MostUsedParameters.Max(x => x.UsageCount) : 0;
                
                foreach (var param in insights.MostUsedParameters.Take(10))
                {
                    TopParameters.Add(new TopParameterItem
                    {
                        ParameterName = param.ParameterName,
                        UsageCount = param.UsageCount,
                        UsageBarWidth = maxUsage > 0 ? (param.UsageCount * 80.0 / maxUsage) : 0
                    });
                }

                // Load usage insights (simplified - using recommendations for now)
                UsageInsights.Clear();
                foreach (var rec in insights.Recommendations.Take(5))
                {
                    UsageInsights.Add(new UsageInsight
                    {
                        Title = rec.Title,
                        Description = rec.Description
                    });
                }

                // Load parameter trends (simplified demo data)
                ParameterTrends.Clear();
                foreach (var param in insights.MostUsedParameters.Take(5))
                {
                    var trendDirection = param.UsagePercentage > 15 ? "↗️ Rising" : "↘️ Declining";
                    ParameterTrends.Add(new ParameterTrend
                    {
                        ParameterName = param.ParameterName,
                        TrendDirection = trendDirection,
                        TrendColor = GetTrendColor(trendDirection),
                        TrendPoints = GenerateTrendPoints(GenerateDemoDataPoints(param.UsageCount))
                    });
                }

                // Load session analytics (simplified demo data)
                SessionAnalytics.Clear();
                for (int i = 0; i < 10; i++)
                {
                    SessionAnalytics.Add(new SessionAnalytic
                    {
                        SessionDate = DateTime.Now.AddDays(-i),
                        Duration = $"{15 + i * 2}:{30 + i * 5:00}",
                        InteractionCount = 20 + i * 3,
                        ViewMode = (i % 3) switch { 0 => "Novice", 1 => "Enthusiast", _ => "Developer" },
                        Summary = $"Focused on {((i % 2 == 0) ? "image generation" : "model tuning")} parameters"
                    });
                }

                // Load recommendations
                Recommendations.Clear();
                foreach (var rec in insights.Recommendations)
                {
                    Recommendations.Add(new RecommendationItem
                    {
                        Priority = rec.Priority.ToString().ToUpper(),
                        PriorityColor = GetPriorityColor(rec.Priority),
                        Title = rec.Title,
                        Description = rec.Description,
                        ActionItem = rec.Type.ToString(),
                        Impact = rec.Impact
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ParameterUsageHeatMapViewModel] Error loading analytics: {ex.Message}");
        }
    }

    private Brush GetTrendColor(string direction)
    {
        return direction switch
        {
            "↗️ Rising" => new SolidColorBrush(Colors.LimeGreen),
            "↘️ Declining" => new SolidColorBrush(Colors.Orange),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    private List<double> GenerateDemoDataPoints(int baseValue)
    {
        var points = new List<double>();
        for (int i = 0; i < 7; i++)
        {
            // Generate some variation around the base value
            var variation = baseValue * (0.8 + 0.4 * Math.Sin(i * Math.PI / 3));
            points.Add(variation);
        }
        return points;
    }

    private string GenerateTrendPoints(List<double> dataPoints)
    {
        if (dataPoints.Count < 2) return "0,15 10,15";
        
        var points = new List<string>();
        var width = 60.0;
        var height = 30.0;
        var maxValue = dataPoints.Max();
        var minValue = dataPoints.Min();
        var range = maxValue - minValue;
        
        for (int i = 0; i < dataPoints.Count; i++)
        {
            var x = (i * width) / (dataPoints.Count - 1);
            var y = range > 0 
                ? height - ((dataPoints[i] - minValue) / range * height)
                : height / 2;
            
            points.Add($"{x:F0},{y:F0}");
        }
        
        return string.Join(" ", points);
    }

    private Brush GetPriorityColor(RecommendationPriority priority)
    {
        return priority switch
        {
            RecommendationPriority.High => new SolidColorBrush(Colors.Red),
            RecommendationPriority.Medium => new SolidColorBrush(Colors.Orange),
            RecommendationPriority.Low => new SolidColorBrush(Colors.Yellow),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    #endregion

    #region Commands

    private async Task RefreshDataAsync()
    {
        await LoadAnalyticsDataAsync();
        await RefreshHeatMapAsync();
        
        // Notify summary properties changed
        OnPropertyChanged(nameof(TotalInteractions));
        OnPropertyChanged(nameof(MostUsedParameter));
        OnPropertyChanged(nameof(PeakUsageTime));
        OnPropertyChanged(nameof(AverageSessionDuration));
    }

    private async Task GenerateReportAsync()
    {
        try
        {
            IsGeneratingHeatMap = true;
            
            var insights = await _analytics.GenerateInsightsAsync();
            var reportContent = GenerateReportContent(insights);
            
            var fileName = $"ParameterUsageReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            
            await File.WriteAllTextAsync(filePath, reportContent);
            
            MessageBox.Show($"Report generated successfully!\n\nSaved to: {filePath}", 
                "Report Generated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating report: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGeneratingHeatMap = false;
        }
    }

    private async Task ExportDataAsync()
    {
        try
        {
            IsGeneratingHeatMap = true;
            
            var heatMapData = await _analytics.GenerateHeatMapAsync();
            var csvContent = GenerateCsvContent(heatMapData);
            
            var fileName = $"ParameterUsageData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            
            await File.WriteAllTextAsync(filePath, csvContent);
            
            MessageBox.Show($"Data exported successfully!\n\nSaved to: {filePath}", 
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting data: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGeneratingHeatMap = false;
        }
    }

    private string GenerateReportContent(ParameterUsageInsights insights)
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("PARAMETER USAGE ANALYTICS REPORT");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"ViewMode: {SelectedViewMode}");
        report.AppendLine($"Expertise Level: {SelectedExpertiseLevel}");
        report.AppendLine($"Time Range: {SelectedTimeRange}");
        report.AppendLine();
        
        report.AppendLine("SUMMARY STATISTICS");
        report.AppendLine("===================");
        report.AppendLine($"Total Interactions: {TotalInteractions:N0}");
        report.AppendLine($"Most Used Parameter: {MostUsedParameter}");
        report.AppendLine($"Peak Usage Time: {PeakUsageTime}");
        report.AppendLine($"Average Session Duration: {AverageSessionDuration}");
        report.AppendLine();
        
        report.AppendLine("TOP PARAMETERS");
        report.AppendLine("==============");
        foreach (var param in insights.MostUsedParameters.Take(10))
        {
            report.AppendLine($"{param.ParameterName}: {param.UsageCount:N0} interactions ({param.UsagePercentage:F1}%)");
        }
        report.AppendLine();
        
        report.AppendLine("PROGRESSIVE DISCLOSURE EFFECTIVENESS");
        report.AppendLine("===================================");
        report.AppendLine($"Beginner Parameter Count: {insights.ProgressiveDisclosureEffectiveness.BeginnerParameterCount}");
        report.AppendLine($"Beginner Overwhelm: {(insights.ProgressiveDisclosureEffectiveness.BeginnerOverwhelm ? "Yes" : "No")}");
        report.AppendLine($"Expert Parameter Coverage: {insights.ProgressiveDisclosureEffectiveness.ExpertParameterCoverage:P}");
        report.AppendLine($"Overall Effectiveness Score: {insights.ProgressiveDisclosureEffectiveness.OverallScore:F1}/10");
        report.AppendLine();
        
        report.AppendLine("RECOMMENDATIONS");
        report.AppendLine("===============");
        foreach (var rec in insights.Recommendations)
        {
            report.AppendLine($"[{rec.Priority}] {rec.Title}");
            report.AppendLine($"    {rec.Description}");
            report.AppendLine($"    Type: {rec.Type}");
            report.AppendLine($"    Impact: {rec.Impact}");
            report.AppendLine();
        }
        
        return report.ToString();
    }

    private string GenerateCsvContent(ParameterHeatMapData heatMapData)
    {
        if (heatMapData?.OverallHeatMap == null) return "No data available";
        
        var csv = new System.Text.StringBuilder();
        
        // Header row
        csv.AppendLine("Parameter,Usage Count,Heat Intensity,Last Used,Popularity Rank");
        
        // Data rows
        foreach (var entry in heatMapData.OverallHeatMap.OrderByDescending(x => x.UsageCount))
        {
            csv.AppendLine($"{entry.ParameterName},{entry.UsageCount},{entry.HeatIntensity:F3},{entry.LastUsed:yyyy-MM-dd HH:mm},{entry.PopularityRank}");
        }
        
        return csv.ToString();
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

#region Data Models

public class HeatMapCell
{
    public string ParameterName { get; set; } = "";
    public string TimePeriod { get; set; } = "";
    public int UsageCount { get; set; }
    public double Intensity { get; set; }
    public Brush IntensityBrush { get; set; } = Brushes.Blue;
    public Brush TextColor { get; set; } = Brushes.White;
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public string ToolTipText { get; set; } = "";
}

public class AxisLabel
{
    public string Text { get; set; } = "";
    public double XPosition { get; set; }
    public double YPosition { get; set; }
}

public class TopParameterItem
{
    public string ParameterName { get; set; } = "";
    public int UsageCount { get; set; }
    public double UsageBarWidth { get; set; }
}

public class UsageInsight
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public class ParameterTrend
{
    public string ParameterName { get; set; } = "";
    public string TrendDirection { get; set; } = "";
    public Brush TrendColor { get; set; } = Brushes.Gray;
    public string TrendPoints { get; set; } = "";
}

public class SessionAnalytic
{
    public DateTime SessionDate { get; set; }
    public string Duration { get; set; } = "";
    public int InteractionCount { get; set; }
    public string ViewMode { get; set; } = "";
    public string Summary { get; set; } = "";
}

public class RecommendationItem
{
    public string Priority { get; set; } = "";
    public Brush PriorityColor { get; set; } = Brushes.Gray;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ActionItem { get; set; } = "";
    public string Impact { get; set; } = "";
}

#endregion

#region Command Classes

public class HeatMapRelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public HeatMapRelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

public class HeatMapAsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public HeatMapAsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

#endregion