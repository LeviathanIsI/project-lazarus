using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using App.Shared.Enums;
using Lazarus.Shared.Models;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Comprehensive parameter usage tracking and analytics system
/// Generates heat maps and usage insights across ViewModes and expertise levels
/// </summary>
public class ParameterUsageAnalytics : INotifyPropertyChanged
{
    private readonly Dictionary<string, ParameterUsageData> _parameterUsage = new();
    private readonly Dictionary<ViewMode, Dictionary<string, int>> _viewModeUsage = new();
    private readonly Dictionary<ExpertiseLevel, Dictionary<string, int>> _expertiseUsage = new();
    private readonly List<ParameterInteractionEvent> _interactionLog = new();
    private readonly object _lockObject = new();
    
    private DateTime _trackingStartTime = DateTime.Now;
    private int _totalInteractions = 0;

    public ParameterUsageAnalytics()
    {
        InitializeTrackingSystem();
        Console.WriteLine($"[ParameterAnalytics] 📊 Parameter usage analytics system initialized");
    }

    #region Usage Tracking

    /// <summary>
    /// Track parameter interaction by user
    /// </summary>
    /// <param name="parameterName">Parameter that was interacted with</param>
    /// <param name="viewMode">Current ViewMode during interaction</param>
    /// <param name="expertiseLevel">User's expertise level</param>
    /// <param name="interactionType">Type of interaction (view, modify, etc.)</param>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New value</param>
    public void TrackParameterUsage(string parameterName, ViewMode viewMode, ExpertiseLevel expertiseLevel, 
        ParameterInteractionType interactionType, object? oldValue = null, object? newValue = null)
    {
        lock (_lockObject)
        {
            try
            {
                var timestamp = DateTime.Now;
                
                // Update parameter usage data
                if (!_parameterUsage.ContainsKey(parameterName))
                {
                    _parameterUsage[parameterName] = new ParameterUsageData
                    {
                        ParameterName = parameterName,
                        FirstUsed = timestamp
                    };
                }

                var usage = _parameterUsage[parameterName];
                usage.TotalInteractions++;
                usage.LastUsed = timestamp;
                usage.InteractionsByType[interactionType] = usage.InteractionsByType.GetValueOrDefault(interactionType, 0) + 1;
                usage.InteractionsByViewMode[viewMode] = usage.InteractionsByViewMode.GetValueOrDefault(viewMode, 0) + 1;
                usage.InteractionsByExpertise[expertiseLevel] = usage.InteractionsByExpertise.GetValueOrDefault(expertiseLevel, 0) + 1;

                // Update ViewMode usage tracking
                if (!_viewModeUsage.ContainsKey(viewMode))
                {
                    _viewModeUsage[viewMode] = new Dictionary<string, int>();
                }
                _viewModeUsage[viewMode][parameterName] = _viewModeUsage[viewMode].GetValueOrDefault(parameterName, 0) + 1;

                // Update expertise level tracking
                if (!_expertiseUsage.ContainsKey(expertiseLevel))
                {
                    _expertiseUsage[expertiseLevel] = new Dictionary<string, int>();
                }
                _expertiseUsage[expertiseLevel][parameterName] = _expertiseUsage[expertiseLevel].GetValueOrDefault(parameterName, 0) + 1;

                // Log detailed interaction
                _interactionLog.Add(new ParameterInteractionEvent
                {
                    Timestamp = timestamp,
                    ParameterName = parameterName,
                    ViewMode = viewMode,
                    ExpertiseLevel = expertiseLevel,
                    InteractionType = interactionType,
                    OldValue = oldValue?.ToString(),
                    NewValue = newValue?.ToString()
                });

                _totalInteractions++;

                Console.WriteLine($"[ParameterAnalytics] 📈 Tracked: {parameterName} {interactionType} in {viewMode} mode ({expertiseLevel} user)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParameterAnalytics] ❌ Failed to track parameter usage: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Track parameter view/visibility without modification
    /// </summary>
    public void TrackParameterView(string parameterName, ViewMode viewMode, ExpertiseLevel expertiseLevel)
    {
        TrackParameterUsage(parameterName, viewMode, expertiseLevel, ParameterInteractionType.View);
    }

    /// <summary>
    /// Track parameter value modification
    /// </summary>
    public void TrackParameterModification(string parameterName, ViewMode viewMode, ExpertiseLevel expertiseLevel, 
        object? oldValue, object? newValue)
    {
        TrackParameterUsage(parameterName, viewMode, expertiseLevel, ParameterInteractionType.Modify, oldValue, newValue);
    }

    /// <summary>
    /// Track parameter help/documentation access
    /// </summary>
    public void TrackParameterHelp(string parameterName, ViewMode viewMode, ExpertiseLevel expertiseLevel)
    {
        TrackParameterUsage(parameterName, viewMode, expertiseLevel, ParameterInteractionType.Help);
    }

    #endregion

    #region Heat Map Generation

    /// <summary>
    /// Generate comprehensive parameter usage heat map
    /// </summary>
    public async Task<ParameterHeatMapData> GenerateHeatMapAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObject)
            {
                Console.WriteLine($"[ParameterAnalytics] 🔥 Generating parameter usage heat map from {_totalInteractions} interactions...");
                
                var heatMapData = new ParameterHeatMapData
                {
                    GeneratedAt = DateTime.Now,
                    TrackingPeriod = DateTime.Now - _trackingStartTime,
                    TotalInteractions = _totalInteractions
                };

                // Generate overall heat map
                heatMapData.OverallHeatMap = GenerateOverallHeatMap();

                // Generate ViewMode-specific heat maps
                foreach (var viewMode in Enum.GetValues<ViewMode>())
                {
                    if (_viewModeUsage.ContainsKey(viewMode))
                    {
                        heatMapData.ViewModeHeatMaps[viewMode] = GenerateViewModeHeatMap(viewMode);
                    }
                }

                // Generate expertise-level heat maps
                foreach (var expertise in Enum.GetValues<ExpertiseLevel>())
                {
                    if (_expertiseUsage.ContainsKey(expertise))
                    {
                        heatMapData.ExpertiseHeatMaps[expertise] = GenerateExpertiseHeatMap(expertise);
                    }
                }

                // Generate interaction type analysis
                heatMapData.InteractionTypeAnalysis = GenerateInteractionTypeAnalysis();

                // Generate trending analysis
                heatMapData.TrendingParameters = GenerateTrendingParameters();

                // Generate correlation analysis
                heatMapData.ParameterCorrelations = GenerateParameterCorrelations();

                Console.WriteLine($"[ParameterAnalytics] 🔥 Heat map generated with {heatMapData.OverallHeatMap.Count} parameters analyzed");
                
                return heatMapData;
            }
        });
    }

    /// <summary>
    /// Generate ViewMode-specific heat map comparison
    /// </summary>
    public async Task<ViewModeComparisonHeatMap> GenerateViewModeComparisonAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObject)
            {
                var comparison = new ViewModeComparisonHeatMap
                {
                    GeneratedAt = DateTime.Now
                };

                var allParameters = _parameterUsage.Keys.ToList();
                
                foreach (var parameter in allParameters)
                {
                    var paramComparison = new ParameterViewModeComparison
                    {
                        ParameterName = parameter
                    };

                    foreach (var viewMode in Enum.GetValues<ViewMode>())
                    {
                        var usage = _viewModeUsage.GetValueOrDefault(viewMode, new Dictionary<string, int>())
                            .GetValueOrDefault(parameter, 0);
                        
                        var totalViewModeUsage = _viewModeUsage.GetValueOrDefault(viewMode, new Dictionary<string, int>())
                            .Values.Sum();

                        var percentage = totalViewModeUsage > 0 ? (double)usage / totalViewModeUsage * 100 : 0;

                        paramComparison.ViewModeUsage[viewMode] = new ViewModeUsageMetrics
                        {
                            AbsoluteCount = usage,
                            PercentageOfViewMode = percentage,
                            RelativeHeat = CalculateRelativeHeat(usage, totalViewModeUsage)
                        };
                    }

                    comparison.ParameterComparisons.Add(paramComparison);
                }

                return comparison;
            }
        });
    }

    #endregion

    #region Analytics & Insights

    /// <summary>
    /// Generate comprehensive usage insights and recommendations
    /// </summary>
    public async Task<ParameterUsageInsights> GenerateInsightsAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObject)
            {
                Console.WriteLine($"[ParameterAnalytics] 🧠 Generating usage insights from {_parameterUsage.Count} tracked parameters...");

                var insights = new ParameterUsageInsights
                {
                    GeneratedAt = DateTime.Now,
                    AnalysisPeriod = DateTime.Now - _trackingStartTime,
                    TotalParametersTracked = _parameterUsage.Count,
                    TotalInteractions = _totalInteractions
                };

                // Most/least used parameters
                var sortedByUsage = _parameterUsage.Values
                    .OrderByDescending(p => p.TotalInteractions)
                    .ToList();

                insights.MostUsedParameters = sortedByUsage.Take(10).Select(p => new ParameterPopularityMetric
                {
                    ParameterName = p.ParameterName,
                    UsageCount = p.TotalInteractions,
                    UsagePercentage = (double)p.TotalInteractions / _totalInteractions * 100
                }).ToList();

                insights.LeastUsedParameters = sortedByUsage.TakeLast(10).Select(p => new ParameterPopularityMetric
                {
                    ParameterName = p.ParameterName,
                    UsageCount = p.TotalInteractions,
                    UsagePercentage = (double)p.TotalInteractions / _totalInteractions * 100
                }).ToList();

                // ViewMode analysis
                insights.ViewModeAnalysis = AnalyzeViewModeUsagePatterns();

                // Expertise level analysis
                insights.ExpertiseAnalysis = AnalyzeExpertiseUsagePatterns();

                // Progressive disclosure effectiveness
                insights.ProgressiveDisclosureEffectiveness = AnalyzeProgressiveDisclosureEffectiveness();

                // Parameter complexity analysis
                insights.ComplexityAnalysis = AnalyzeParameterComplexity();

                // Generate actionable recommendations
                insights.Recommendations = GenerateUsageRecommendations(insights);

                Console.WriteLine($"[ParameterAnalytics] 🧠 Generated {insights.Recommendations.Count} actionable insights");

                return insights;
            }
        });
    }

    /// <summary>
    /// Generate parameter usage report for export
    /// </summary>
    public async Task<ParameterUsageReport> GenerateUsageReportAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObject)
            {
                var report = new ParameterUsageReport
                {
                    GeneratedAt = DateTime.Now,
                    TrackingStartTime = _trackingStartTime,
                    TrackingDuration = DateTime.Now - _trackingStartTime,
                    TotalInteractions = _totalInteractions,
                    TotalParametersTracked = _parameterUsage.Count,
                    UniqueViewModes = _viewModeUsage.Keys.Count,
                    UniqueExpertiseLevels = _expertiseUsage.Keys.Count
                };

                // Detailed parameter statistics
                report.ParameterStatistics = _parameterUsage.Values.Select(p => new ParameterStatistics
                {
                    ParameterName = p.ParameterName,
                    TotalInteractions = p.TotalInteractions,
                    FirstUsed = p.FirstUsed,
                    LastUsed = p.LastUsed,
                    UsageFrequency = p.TotalInteractions / Math.Max(1, (DateTime.Now - p.FirstUsed).TotalHours),
                    InteractionTypeBreakdown = p.InteractionsByType.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                    ViewModeBreakdown = p.InteractionsByViewMode.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                    ExpertiseBreakdown = p.InteractionsByExpertise.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)
                }).OrderByDescending(p => p.TotalInteractions).ToList();

                // Summary metrics
                report.SummaryMetrics = new UsageSummaryMetrics
                {
                    AverageInteractionsPerParameter = (double)_totalInteractions / _parameterUsage.Count,
                    MostActiveViewMode = _viewModeUsage.OrderByDescending(vm => vm.Value.Values.Sum()).FirstOrDefault().Key.ToString(),
                    MostActiveExpertiseLevel = _expertiseUsage.OrderByDescending(e => e.Value.Values.Sum()).FirstOrDefault().Key.ToString(),
                    PeakUsageHour = CalculatePeakUsageHour(),
                    ParameterDiscoveryRate = CalculateParameterDiscoveryRate()
                };

                return report;
            }
        });
    }

    #endregion

    #region Helper Methods

    private void InitializeTrackingSystem()
    {
        // Initialize tracking for common parameters with baseline data
        var commonParameters = new[]
        {
            "Temperature", "TopP", "TopK", "RepetitionPenalty", "Mirostat", 
            "TailFreeSampling", "TypicalP", "LocallyTypical", "DynamicTemperature"
        };

        foreach (var param in commonParameters)
        {
            _parameterUsage[param] = new ParameterUsageData
            {
                ParameterName = param,
                FirstUsed = DateTime.Now
            };
        }
    }

    private List<HeatMapEntry> GenerateOverallHeatMap()
    {
        var heatMap = new List<HeatMapEntry>();
        var maxUsage = _parameterUsage.Values.Max(p => p.TotalInteractions);

        foreach (var param in _parameterUsage.Values.OrderByDescending(p => p.TotalInteractions))
        {
            heatMap.Add(new HeatMapEntry
            {
                ParameterName = param.ParameterName,
                UsageCount = param.TotalInteractions,
                HeatIntensity = (double)param.TotalInteractions / maxUsage,
                LastUsed = param.LastUsed,
                PopularityRank = heatMap.Count + 1
            });
        }

        return heatMap;
    }

    private List<HeatMapEntry> GenerateViewModeHeatMap(ViewMode viewMode)
    {
        var heatMap = new List<HeatMapEntry>();
        var viewModeUsage = _viewModeUsage[viewMode];
        var maxUsage = viewModeUsage.Values.Max();

        foreach (var param in viewModeUsage.OrderByDescending(p => p.Value))
        {
            heatMap.Add(new HeatMapEntry
            {
                ParameterName = param.Key,
                UsageCount = param.Value,
                HeatIntensity = (double)param.Value / maxUsage,
                LastUsed = _parameterUsage[param.Key].LastUsed,
                PopularityRank = heatMap.Count + 1
            });
        }

        return heatMap;
    }

    private List<HeatMapEntry> GenerateExpertiseHeatMap(ExpertiseLevel expertiseLevel)
    {
        var heatMap = new List<HeatMapEntry>();
        var expertiseUsage = _expertiseUsage[expertiseLevel];
        var maxUsage = expertiseUsage.Values.Max();

        foreach (var param in expertiseUsage.OrderByDescending(p => p.Value))
        {
            heatMap.Add(new HeatMapEntry
            {
                ParameterName = param.Key,
                UsageCount = param.Value,
                HeatIntensity = (double)param.Value / maxUsage,
                LastUsed = _parameterUsage[param.Key].LastUsed,
                PopularityRank = heatMap.Count + 1
            });
        }

        return heatMap;
    }

    private Dictionary<ParameterInteractionType, InteractionTypeMetrics> GenerateInteractionTypeAnalysis()
    {
        var analysis = new Dictionary<ParameterInteractionType, InteractionTypeMetrics>();

        foreach (var interactionType in Enum.GetValues<ParameterInteractionType>())
        {
            var totalInteractions = _parameterUsage.Values
                .Sum(p => p.InteractionsByType.GetValueOrDefault(interactionType, 0));

            analysis[interactionType] = new InteractionTypeMetrics
            {
                TotalInteractions = totalInteractions,
                Percentage = (double)totalInteractions / _totalInteractions * 100,
                MostUsedParameter = _parameterUsage.Values
                    .OrderByDescending(p => p.InteractionsByType.GetValueOrDefault(interactionType, 0))
                    .FirstOrDefault()?.ParameterName ?? "None"
            };
        }

        return analysis;
    }

    private List<TrendingParameterMetric> GenerateTrendingParameters()
    {
        var trending = new List<TrendingParameterMetric>();
        var now = DateTime.Now;
        var recentThreshold = now.AddHours(-24); // Last 24 hours

        foreach (var param in _parameterUsage.Values)
        {
            var recentInteractions = _interactionLog
                .Count(i => i.ParameterName == param.ParameterName && i.Timestamp >= recentThreshold);

            var historicalAverage = param.TotalInteractions / Math.Max(1, (now - param.FirstUsed).TotalDays);
            var recentAverage = recentInteractions / 1.0; // Per day

            var trendScore = recentAverage > 0 ? recentAverage / Math.Max(0.1, historicalAverage) : 0;

            trending.Add(new TrendingParameterMetric
            {
                ParameterName = param.ParameterName,
                RecentInteractions = recentInteractions,
                TrendScore = trendScore,
                TrendDirection = trendScore > 1.2 ? "Rising" : trendScore < 0.8 ? "Declining" : "Stable"
            });
        }

        return trending.OrderByDescending(t => t.TrendScore).Take(10).ToList();
    }

    private List<ParameterCorrelation> GenerateParameterCorrelations()
    {
        var correlations = new List<ParameterCorrelation>();
        var parameters = _parameterUsage.Keys.ToList();

        for (int i = 0; i < parameters.Count; i++)
        {
            for (int j = i + 1; j < parameters.Count; j++)
            {
                var param1 = parameters[i];
                var param2 = parameters[j];

                var coUsageCount = _interactionLog.Count(interaction =>
                {
                    var sameSession = _interactionLog.Any(other =>
                        other.ParameterName == param2 &&
                        Math.Abs((other.Timestamp - interaction.Timestamp).TotalMinutes) <= 5);
                    
                    return interaction.ParameterName == param1 && sameSession;
                });

                var correlation = (double)coUsageCount / Math.Min(
                    _parameterUsage[param1].TotalInteractions,
                    _parameterUsage[param2].TotalInteractions);

                if (correlation > 0.1) // Only include meaningful correlations
                {
                    correlations.Add(new ParameterCorrelation
                    {
                        Parameter1 = param1,
                        Parameter2 = param2,
                        CorrelationStrength = correlation,
                        CoUsageCount = coUsageCount
                    });
                }
            }
        }

        return correlations.OrderByDescending(c => c.CorrelationStrength).Take(20).ToList();
    }

    private double CalculateRelativeHeat(int usage, int totalUsage)
    {
        return totalUsage > 0 ? (double)usage / totalUsage : 0;
    }

    private ViewModeUsageAnalysis AnalyzeViewModeUsagePatterns()
    {
        var analysis = new ViewModeUsageAnalysis();

        foreach (var viewMode in _viewModeUsage.Keys)
        {
            var totalUsage = _viewModeUsage[viewMode].Values.Sum();
            var uniqueParameters = _viewModeUsage[viewMode].Count;
            var avgUsagePerParam = (double)totalUsage / uniqueParameters;

            analysis.ViewModeMetrics[viewMode] = new ViewModeMetrics
            {
                TotalInteractions = totalUsage,
                UniqueParametersUsed = uniqueParameters,
                AverageUsagePerParameter = avgUsagePerParam,
                MostUsedParameter = _viewModeUsage[viewMode]
                    .OrderByDescending(p => p.Value)
                    .FirstOrDefault().Key
            };
        }

        return analysis;
    }

    private ExpertiseUsageAnalysis AnalyzeExpertiseUsagePatterns()
    {
        var analysis = new ExpertiseUsageAnalysis();

        foreach (var expertise in _expertiseUsage.Keys)
        {
            var totalUsage = _expertiseUsage[expertise].Values.Sum();
            var uniqueParameters = _expertiseUsage[expertise].Count;

            analysis.ExpertiseMetrics[expertise] = new ExpertiseMetrics
            {
                TotalInteractions = totalUsage,
                UniqueParametersUsed = uniqueParameters,
                ParameterDiversityScore = (double)uniqueParameters / _parameterUsage.Count,
                PreferredParameters = _expertiseUsage[expertise]
                    .OrderByDescending(p => p.Value)
                    .Take(5)
                    .Select(p => p.Key)
                    .ToList()
            };
        }

        return analysis;
    }

    private ProgressiveDisclosureEffectiveness AnalyzeProgressiveDisclosureEffectiveness()
    {
        var effectiveness = new ProgressiveDisclosureEffectiveness();

        // Analyze if beginners are overwhelmed with too many parameters
        var beginnerParameters = _expertiseUsage.GetValueOrDefault(ExpertiseLevel.Beginner, new Dictionary<string, int>());
        effectiveness.BeginnerParameterCount = beginnerParameters.Count;
        effectiveness.BeginnerOverwhelm = beginnerParameters.Count > 7; // Miller's Rule

        // Analyze if advanced users find what they need
        var expertParameters = _expertiseUsage.GetValueOrDefault(ExpertiseLevel.Expert, new Dictionary<string, int>());
        var availableAdvancedParams = _parameterUsage.Keys.Count(p => 
            ParameterRiskRegistry.GetClassification(p)?.RiskLevel == ParameterRiskLevel.Experimental);
        
        effectiveness.ExpertParameterCoverage = availableAdvancedParams > 0 ? 
            (double)expertParameters.Count / availableAdvancedParams : 1.0;

        // Overall effectiveness score
        effectiveness.OverallScore = CalculateOverallEffectivenessScore();

        return effectiveness;
    }

    private ComplexityAnalysis AnalyzeParameterComplexity()
    {
        var analysis = new ComplexityAnalysis();

        foreach (var param in _parameterUsage.Keys)
        {
            var classification = ParameterRiskRegistry.GetClassification(param);
            var riskLevel = classification?.RiskLevel ?? ParameterRiskLevel.Safe;

            if (!analysis.ComplexityUsage.ContainsKey(riskLevel))
            {
                analysis.ComplexityUsage[riskLevel] = new ComplexityUsageMetrics();
            }

            var metrics = analysis.ComplexityUsage[riskLevel];
            metrics.ParameterCount++;
            metrics.TotalInteractions += _parameterUsage[param].TotalInteractions;
        }

        return analysis;
    }

    private List<UsageRecommendation> GenerateUsageRecommendations(ParameterUsageInsights insights)
    {
        var recommendations = new List<UsageRecommendation>();

        // Recommend moving popular parameters up in UI
        if (insights.MostUsedParameters.Any())
        {
            var topParam = insights.MostUsedParameters.First();
            if (topParam.UsagePercentage > 30)
            {
                recommendations.Add(new UsageRecommendation
                {
                    Type = RecommendationType.UIOptimization,
                    Priority = RecommendationPriority.High,
                    Title = $"Prioritize {topParam.ParameterName} Visibility",
                    Description = $"{topParam.ParameterName} accounts for {topParam.UsagePercentage:F1}% of all interactions. Consider making it more prominent in the UI.",
                    Impact = "Reduced cognitive load and faster task completion"
                });
            }
        }

        // Recommend hiding rarely used parameters
        var underutilized = insights.LeastUsedParameters
            .Where(p => p.UsagePercentage < 1.0)
            .ToList();

        if (underutilized.Count > 3)
        {
            recommendations.Add(new UsageRecommendation
            {
                Type = RecommendationType.ParameterHiding,
                Priority = RecommendationPriority.Medium,
                Title = "Consider Moving Low-Usage Parameters to Advanced Sections",
                Description = $"{underutilized.Count} parameters have less than 1% usage. Consider moving them to expandable advanced sections.",
                Impact = "Reduced UI clutter and improved focus on commonly used parameters"
            });
        }

        // Progressive disclosure effectiveness recommendations
        if (insights.ProgressiveDisclosureEffectiveness.BeginnerOverwhelm)
        {
            recommendations.Add(new UsageRecommendation
            {
                Type = RecommendationType.CognitiveLoad,
                Priority = RecommendationPriority.High,
                Title = "Reduce Parameter Exposure for Beginners",
                Description = $"Beginners are exposed to {insights.ProgressiveDisclosureEffectiveness.BeginnerParameterCount} parameters, exceeding Miller's Rule (7±2). Consider hiding more advanced parameters in Novice mode.",
                Impact = "Improved user experience and reduced abandonment for new users"
            });
        }

        return recommendations;
    }

    private int CalculatePeakUsageHour()
    {
        return _interactionLog
            .GroupBy(i => i.Timestamp.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? 12;
    }

    private double CalculateParameterDiscoveryRate()
    {
        var totalDays = Math.Max(1, (DateTime.Now - _trackingStartTime).TotalDays);
        return _parameterUsage.Count / totalDays;
    }

    private double CalculateOverallEffectivenessScore()
    {
        // Composite score based on various factors
        var score = 0.0;

        // Factor 1: Parameter distribution balance (40%)
        var usageVariance = CalculateUsageVariance();
        var balanceScore = Math.Max(0, 1.0 - usageVariance / 100.0);
        score += balanceScore * 0.4;

        // Factor 2: Expertise level satisfaction (35%)
        var expertiseScore = CalculateExpertiseSatisfactionScore();
        score += expertiseScore * 0.35;

        // Factor 3: Cognitive load compliance (25%)
        var cognitiveScore = CalculateCognitiveLoadScore();
        score += cognitiveScore * 0.25;

        return Math.Min(1.0, Math.Max(0.0, score));
    }

    private double CalculateUsageVariance()
    {
        var usages = _parameterUsage.Values.Select(p => (double)p.TotalInteractions).ToList();
        var mean = usages.Average();
        var variance = usages.Sum(u => Math.Pow(u - mean, 2)) / usages.Count;
        return Math.Sqrt(variance);
    }

    private double CalculateExpertiseSatisfactionScore()
    {
        // Simplified calculation - in practice, this would be more sophisticated
        var beginnerSatisfaction = _expertiseUsage.GetValueOrDefault(ExpertiseLevel.Beginner, new Dictionary<string, int>()).Count <= 7 ? 1.0 : 0.7;
        var expertSatisfaction = _expertiseUsage.GetValueOrDefault(ExpertiseLevel.Expert, new Dictionary<string, int>()).Count >= 10 ? 1.0 : 0.8;
        return (beginnerSatisfaction + expertSatisfaction) / 2.0;
    }

    private double CalculateCognitiveLoadScore()
    {
        // Based on Miller's Rule compliance
        var noviceParams = _viewModeUsage.GetValueOrDefault(ViewMode.Novice, new Dictionary<string, int>()).Count;
        return noviceParams <= 7 ? 1.0 : Math.Max(0.0, 1.0 - (noviceParams - 7) * 0.1);
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

#region Data Structures

public class ParameterUsageData
{
    public string ParameterName { get; set; } = "";
    public int TotalInteractions { get; set; }
    public DateTime FirstUsed { get; set; }
    public DateTime LastUsed { get; set; }
    public Dictionary<ParameterInteractionType, int> InteractionsByType { get; set; } = new();
    public Dictionary<ViewMode, int> InteractionsByViewMode { get; set; } = new();
    public Dictionary<ExpertiseLevel, int> InteractionsByExpertise { get; set; } = new();
}

public class ParameterInteractionEvent
{
    public DateTime Timestamp { get; set; }
    public string ParameterName { get; set; } = "";
    public ViewMode ViewMode { get; set; }
    public ExpertiseLevel ExpertiseLevel { get; set; }
    public ParameterInteractionType InteractionType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public enum ParameterInteractionType
{
    View = 1,
    Modify = 2,
    Help = 3,
    Reset = 4,
    Copy = 5,
    Paste = 6
}


public class ParameterHeatMapData
{
    public DateTime GeneratedAt { get; set; }
    public TimeSpan TrackingPeriod { get; set; }
    public int TotalInteractions { get; set; }
    public List<HeatMapEntry> OverallHeatMap { get; set; } = new();
    public Dictionary<ViewMode, List<HeatMapEntry>> ViewModeHeatMaps { get; set; } = new();
    public Dictionary<ExpertiseLevel, List<HeatMapEntry>> ExpertiseHeatMaps { get; set; } = new();
    public Dictionary<ParameterInteractionType, InteractionTypeMetrics> InteractionTypeAnalysis { get; set; } = new();
    public List<TrendingParameterMetric> TrendingParameters { get; set; } = new();
    public List<ParameterCorrelation> ParameterCorrelations { get; set; } = new();
}

public class HeatMapEntry
{
    public string ParameterName { get; set; } = "";
    public int UsageCount { get; set; }
    public double HeatIntensity { get; set; } // 0.0 to 1.0
    public DateTime LastUsed { get; set; }
    public int PopularityRank { get; set; }
}

public class ViewModeComparisonHeatMap
{
    public DateTime GeneratedAt { get; set; }
    public List<ParameterViewModeComparison> ParameterComparisons { get; set; } = new();
}

public class ParameterViewModeComparison
{
    public string ParameterName { get; set; } = "";
    public Dictionary<ViewMode, ViewModeUsageMetrics> ViewModeUsage { get; set; } = new();
}

public class ViewModeUsageMetrics
{
    public int AbsoluteCount { get; set; }
    public double PercentageOfViewMode { get; set; }
    public double RelativeHeat { get; set; }
}

public class InteractionTypeMetrics
{
    public int TotalInteractions { get; set; }
    public double Percentage { get; set; }
    public string MostUsedParameter { get; set; } = "";
}

public class TrendingParameterMetric
{
    public string ParameterName { get; set; } = "";
    public int RecentInteractions { get; set; }
    public double TrendScore { get; set; }
    public string TrendDirection { get; set; } = "";
}

public class ParameterCorrelation
{
    public string Parameter1 { get; set; } = "";
    public string Parameter2 { get; set; } = "";
    public double CorrelationStrength { get; set; }
    public int CoUsageCount { get; set; }
}

public class ParameterUsageInsights
{
    public DateTime GeneratedAt { get; set; }
    public TimeSpan AnalysisPeriod { get; set; }
    public int TotalParametersTracked { get; set; }
    public int TotalInteractions { get; set; }
    public List<ParameterPopularityMetric> MostUsedParameters { get; set; } = new();
    public List<ParameterPopularityMetric> LeastUsedParameters { get; set; } = new();
    public ViewModeUsageAnalysis ViewModeAnalysis { get; set; } = new();
    public ExpertiseUsageAnalysis ExpertiseAnalysis { get; set; } = new();
    public ProgressiveDisclosureEffectiveness ProgressiveDisclosureEffectiveness { get; set; } = new();
    public ComplexityAnalysis ComplexityAnalysis { get; set; } = new();
    public List<UsageRecommendation> Recommendations { get; set; } = new();
}

public class ParameterPopularityMetric
{
    public string ParameterName { get; set; } = "";
    public int UsageCount { get; set; }
    public double UsagePercentage { get; set; }
}

public class ViewModeUsageAnalysis
{
    public Dictionary<ViewMode, ViewModeMetrics> ViewModeMetrics { get; set; } = new();
}

public class ViewModeMetrics
{
    public int TotalInteractions { get; set; }
    public int UniqueParametersUsed { get; set; }
    public double AverageUsagePerParameter { get; set; }
    public string MostUsedParameter { get; set; } = "";
}

public class ExpertiseUsageAnalysis
{
    public Dictionary<ExpertiseLevel, ExpertiseMetrics> ExpertiseMetrics { get; set; } = new();
}

public class ExpertiseMetrics
{
    public int TotalInteractions { get; set; }
    public int UniqueParametersUsed { get; set; }
    public double ParameterDiversityScore { get; set; }
    public List<string> PreferredParameters { get; set; } = new();
}

public class ProgressiveDisclosureEffectiveness
{
    public int BeginnerParameterCount { get; set; }
    public bool BeginnerOverwhelm { get; set; }
    public double ExpertParameterCoverage { get; set; }
    public double OverallScore { get; set; }
}

public class ComplexityAnalysis
{
    public Dictionary<ParameterRiskLevel, ComplexityUsageMetrics> ComplexityUsage { get; set; } = new();
}

public class ComplexityUsageMetrics
{
    public int ParameterCount { get; set; }
    public int TotalInteractions { get; set; }
}

public class UsageRecommendation
{
    public RecommendationType Type { get; set; }
    public RecommendationPriority Priority { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Impact { get; set; } = "";
}

public enum RecommendationType
{
    UIOptimization,
    ParameterHiding,
    CognitiveLoad,
    UserExperience,
    Performance
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class ParameterUsageReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime TrackingStartTime { get; set; }
    public TimeSpan TrackingDuration { get; set; }
    public int TotalInteractions { get; set; }
    public int TotalParametersTracked { get; set; }
    public int UniqueViewModes { get; set; }
    public int UniqueExpertiseLevels { get; set; }
    public List<ParameterStatistics> ParameterStatistics { get; set; } = new();
    public UsageSummaryMetrics SummaryMetrics { get; set; } = new();
}

public class ParameterStatistics
{
    public string ParameterName { get; set; } = "";
    public int TotalInteractions { get; set; }
    public DateTime FirstUsed { get; set; }
    public DateTime LastUsed { get; set; }
    public double UsageFrequency { get; set; }
    public Dictionary<string, int> InteractionTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> ViewModeBreakdown { get; set; } = new();
    public Dictionary<string, int> ExpertiseBreakdown { get; set; } = new();
}

public class UsageSummaryMetrics
{
    public double AverageInteractionsPerParameter { get; set; }
    public string MostActiveViewMode { get; set; } = "";
    public string MostActiveExpertiseLevel { get; set; } = "";
    public int PeakUsageHour { get; set; }
    public double ParameterDiscoveryRate { get; set; }
}

#endregion