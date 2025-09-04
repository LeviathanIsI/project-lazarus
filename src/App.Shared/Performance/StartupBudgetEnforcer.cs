using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Startup Budget Enforcer - Validates application startup performance against defined budgets
/// </summary>
public class StartupBudgetEnforcer
{
    private readonly ILogger<StartupBudgetEnforcer> _logger;
    private readonly Stopwatch _startupStopwatch;
    private readonly Dictionary<string, long> _milestones = new();
    private readonly List<StartupPhase> _phases = new();

    public StartupBudgetEnforcer(ILogger<StartupBudgetEnforcer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _startupStopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Startup budget enforcer initialized - tracking startup performance");
    }

    /// <summary>
    /// Record a startup milestone
    /// </summary>
    public void RecordMilestone(string milestoneName)
    {
        if (_startupStopwatch.IsRunning)
        {
            var elapsedMs = _startupStopwatch.ElapsedMilliseconds;
            _milestones[milestoneName] = elapsedMs;
            
            _logger.LogInformation("Startup milestone: {Milestone} at {Elapsed}ms", 
                milestoneName, elapsedMs);
        }
    }

    /// <summary>
    /// Start tracking a startup phase
    /// </summary>
    public StartupPhaseTracker StartPhase(string phaseName, int budgetMs = 0)
    {
        return new StartupPhaseTracker(this, phaseName, budgetMs);
    }

    /// <summary>
    /// Complete startup tracking and generate report
    /// </summary>
    public Task<StartupReport> CompleteStartupValidationAsync()
    {
        _startupStopwatch.Stop();
        
        var totalTime = _startupStopwatch.ElapsedMilliseconds;
        var budgetViolations = new List<BudgetViolation>();

        // Check total startup time
        if (totalTime > ResourceBudgets.MaxStartupTime)
        {
            budgetViolations.Add(new BudgetViolation
            {
                Type = ViolationType.StartupBudget,
                Severity = ViolationSeverity.Critical,
                Message = $"Total startup time {totalTime}ms exceeds budget {ResourceBudgets.MaxStartupTime}ms",
                CurrentValue = totalTime,
                BudgetLimit = ResourceBudgets.MaxStartupTime,
                Component = "Application Startup"
            });
        }

        // Check individual milestone budgets
        foreach (var milestone in _milestones)
        {
            var budget = GetMilestoneBudget(milestone.Key);
            if (budget > 0 && milestone.Value > budget)
            {
                budgetViolations.Add(new BudgetViolation
                {
                    Type = ViolationType.StartupBudget,
                    Severity = ViolationSeverity.Warning,
                    Message = $"Milestone '{milestone.Key}' took {milestone.Value}ms, exceeds budget {budget}ms",
                    CurrentValue = milestone.Value,
                    BudgetLimit = budget,
                    Component = milestone.Key
                });
            }
        }

        // Check phase budgets
        foreach (var phase in _phases.Where(p => p.BudgetMs > 0))
        {
            if (phase.ElapsedMs > phase.BudgetMs)
            {
                budgetViolations.Add(new BudgetViolation
                {
                    Type = ViolationType.StartupBudget,
                    Severity = phase.ElapsedMs > phase.BudgetMs * 2 ? ViolationSeverity.Critical : ViolationSeverity.Warning,
                    Message = $"Phase '{phase.Name}' took {phase.ElapsedMs}ms, exceeds budget {phase.BudgetMs}ms",
                    CurrentValue = phase.ElapsedMs,
                    BudgetLimit = phase.BudgetMs,
                    Component = $"Phase: {phase.Name}"
                });
            }
        }

        var report = new StartupReport
        {
            TotalStartupTime = totalTime,
            Milestones = new Dictionary<string, long>(_milestones),
            Phases = _phases.ToList(),
            BudgetViolations = budgetViolations,
            IsWithinBudget = !budgetViolations.Any(v => v.Severity == ViolationSeverity.Critical),
            StartupGrade = CalculateStartupGrade(totalTime, budgetViolations),
            CompletedAt = DateTime.UtcNow
        };

        // Log startup summary
        _logger.LogInformation("Startup completed in {TotalTime}ms - Grade: {Grade}, Violations: {Violations}",
            totalTime, report.StartupGrade, budgetViolations.Count);

        if (budgetViolations.Any())
        {
            foreach (var violation in budgetViolations)
            {
                var logLevel = violation.Severity == ViolationSeverity.Critical ? LogLevel.Error : LogLevel.Warning;
                _logger.Log(logLevel, "Startup budget violation: {Message}", violation.Message);
            }
        }

        return Task.FromResult(report);
    }

    internal void CompletePhase(string phaseName, long elapsedMs, int budgetMs)
    {
        _phases.Add(new StartupPhase
        {
            Name = phaseName,
            ElapsedMs = elapsedMs,
            BudgetMs = budgetMs,
            CompletedAt = DateTime.UtcNow
        });

        if (budgetMs > 0 && elapsedMs > budgetMs)
        {
            _logger.LogWarning("Startup phase budget violation: {Phase} took {Elapsed}ms > {Budget}ms",
                phaseName, elapsedMs, budgetMs);
        }
        else
        {
            _logger.LogInformation("Startup phase completed: {Phase} in {Elapsed}ms", 
                phaseName, elapsedMs);
        }
    }

    private int GetMilestoneBudget(string milestoneName)
    {
        // Define milestone budgets
        return milestoneName.ToLowerInvariant() switch
        {
            "application init" => 1000, // 1 second
            "ui rendering" => 2000,     // 2 seconds
            "services init" => 1500,    // 1.5 seconds
            "database init" => 500,     // 500ms
            "theme loading" => 300,     // 300ms
            _ => 0 // No budget defined
        };
    }

    private StartupGrade CalculateStartupGrade(long totalTime, List<BudgetViolation> violations)
    {
        var score = 100;

        // Deduct for total time
        if (totalTime > ResourceBudgets.MaxStartupTime)
        {
            var overtime = totalTime - ResourceBudgets.MaxStartupTime;
            score -= (int)(overtime * 0.01); // 1 point per 10ms over budget
        }

        // Deduct for violations
        score -= violations.Count(v => v.Severity == ViolationSeverity.Critical) * 20;
        score -= violations.Count(v => v.Severity == ViolationSeverity.Warning) * 10;

        return score switch
        {
            >= 90 => StartupGrade.Excellent,
            >= 75 => StartupGrade.Good,
            >= 60 => StartupGrade.Fair,
            >= 40 => StartupGrade.Poor,
            _ => StartupGrade.Critical
        };
    }
}

/// <summary>
/// Startup phase tracker for measuring individual phases
/// </summary>
public class StartupPhaseTracker : IDisposable
{
    private readonly StartupBudgetEnforcer _enforcer;
    private readonly string _phaseName;
    private readonly int _budgetMs;
    private readonly Stopwatch _phaseStopwatch;
    private bool _disposed = false;

    internal StartupPhaseTracker(StartupBudgetEnforcer enforcer, string phaseName, int budgetMs)
    {
        _enforcer = enforcer;
        _phaseName = phaseName;
        _budgetMs = budgetMs;
        _phaseStopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _phaseStopwatch.Stop();
            _enforcer.CompletePhase(_phaseName, _phaseStopwatch.ElapsedMilliseconds, _budgetMs);
        }
    }
}

/// <summary>
/// Startup performance report
/// </summary>
public record StartupReport
{
    public long TotalStartupTime { get; init; }
    public Dictionary<string, long> Milestones { get; init; } = new();
    public List<StartupPhase> Phases { get; init; } = new();
    public List<BudgetViolation> BudgetViolations { get; init; } = new();
    public bool IsWithinBudget { get; init; }
    public StartupGrade StartupGrade { get; init; }
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Individual startup phase
/// </summary>
public record StartupPhase
{
    public string Name { get; init; } = string.Empty;
    public long ElapsedMs { get; init; }
    public int BudgetMs { get; init; }
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Startup performance grade
/// </summary>
public enum StartupGrade
{
    Critical,
    Poor,
    Fair,
    Good,
    Excellent
}