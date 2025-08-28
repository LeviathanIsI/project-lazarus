using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using App.Shared.Enums;
using Lazarus.Shared.Models;

namespace Lazarus.Desktop.Views.Controls;

/// <summary>
/// User control for displaying parameters with risk classification and safety guidance
/// </summary>
public partial class RiskClassifiedParameterControl : UserControl
{
    public RiskClassifiedParameterControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    #region Dependency Properties

    public static readonly DependencyProperty ParameterNameProperty =
        DependencyProperty.Register(nameof(ParameterName), typeof(string), typeof(RiskClassifiedParameterControl),
            new PropertyMetadata(string.Empty, OnParameterNameChanged));

    public static readonly DependencyProperty ParameterControlProperty =
        DependencyProperty.Register(nameof(ParameterControl), typeof(object), typeof(RiskClassifiedParameterControl));

    public static readonly DependencyProperty CurrentViewModeProperty =
        DependencyProperty.Register(nameof(CurrentViewMode), typeof(ViewMode), typeof(RiskClassifiedParameterControl),
            new PropertyMetadata(ViewMode.Novice, OnViewModeChanged));

    public string ParameterName
    {
        get => (string)GetValue(ParameterNameProperty);
        set => SetValue(ParameterNameProperty, value);
    }

    public object ParameterControl
    {
        get => GetValue(ParameterControlProperty);
        set => SetValue(ParameterControlProperty, value);
    }

    public ViewMode CurrentViewMode
    {
        get => (ViewMode)GetValue(CurrentViewModeProperty);
        set => SetValue(CurrentViewModeProperty, value);
    }

    #endregion

    #region Computed Properties

    private ParameterRiskClassification? _classification;

    public ParameterRiskClassification? Classification
    {
        get => _classification;
        private set
        {
            _classification = value;
            UpdateDisplayProperties();
        }
    }

    public string RiskLevel => Classification?.RiskLevel.ToString() ?? "Unknown";
    public string Category => Classification?.Category.ToString() ?? "Unknown";
    public string RiskIcon => Classification?.GetRiskIcon() ?? "❓";
    public string Description => Classification?.Description ?? "No description available";
    public string SafeUsageGuideline => Classification?.SafeUsageGuideline ?? "";
    public string RiskExplanation => Classification?.RiskExplanation ?? "";
    public string RecommendedFor => Classification?.RecommendedFor ?? "";
    public string AvoidWhen => Classification?.AvoidWhen ?? "";
    public List<string> CommonMistakes => Classification?.CommonMistakes ?? new List<string>();
    public List<string> InteractsWith => Classification?.InteractsWith ?? new List<string>();

    public bool HasSafeUsage => !string.IsNullOrEmpty(SafeUsageGuideline);
    public bool HasRiskExplanation => !string.IsNullOrEmpty(RiskExplanation);
    public bool HasCommonMistakes => CommonMistakes.Any();
    public bool HasInteractions => InteractsWith.Any();
    public bool HasRecommendedFor => !string.IsNullOrEmpty(RecommendedFor);
    public bool HasAvoidWhen => !string.IsNullOrEmpty(AvoidWhen);
    public bool HasRecommendations => HasRecommendedFor || HasAvoidWhen;

    public string? WarningMessage => ParameterRiskRegistry.GetWarningForLevel(ParameterName, CurrentViewMode);
    public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);

    public Brush RiskBackgroundBrush
    {
        get
        {
            var colorHex = Classification?.GetRiskColor() ?? "#757575";
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }
    }

    public Brush RiskBorderBrush
    {
        get
        {
            var classification = Classification;
            if (classification == null) return new SolidColorBrush(Colors.Gray);

            // More prominent border for higher risk parameters
            var opacity = classification.RiskLevel switch
            {
                ParameterRiskLevel.Safe => 0.3,
                ParameterRiskLevel.Caution => 0.6,
                ParameterRiskLevel.Experimental => 0.9,
                _ => 0.3
            };

            var colorHex = classification.GetRiskColor();
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            color.A = (byte)(255 * opacity);
            return new SolidColorBrush(color);
        }
    }

    #endregion

    #region Event Handlers

    private static void OnParameterNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RiskClassifiedParameterControl control)
        {
            control.LoadClassification();
        }
    }

    private static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RiskClassifiedParameterControl control)
        {
            control.UpdateDisplayProperties();
        }
    }

    #endregion

    #region Private Methods

    private void LoadClassification()
    {
        if (string.IsNullOrEmpty(ParameterName)) return;

        Classification = ParameterRiskRegistry.GetClassification(ParameterName);
    }

    private void UpdateDisplayProperties()
    {
        // Force property change notifications
        var properties = new[]
        {
            nameof(RiskLevel), nameof(Category), nameof(RiskIcon), nameof(Description),
            nameof(SafeUsageGuideline), nameof(RiskExplanation), nameof(RecommendedFor),
            nameof(AvoidWhen), nameof(CommonMistakes), nameof(InteractsWith),
            nameof(HasSafeUsage), nameof(HasRiskExplanation), nameof(HasCommonMistakes),
            nameof(HasInteractions), nameof(HasRecommendedFor), nameof(HasAvoidWhen),
            nameof(HasRecommendations), nameof(WarningMessage), nameof(HasWarning),
            nameof(RiskBackgroundBrush), nameof(RiskBorderBrush)
        };

        foreach (var property in properties)
        {
            OnPropertyChanged(property);
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        // Simple property change notification for data binding
        var bindingExpression = GetBindingExpression(DataContextProperty);
        bindingExpression?.UpdateTarget();
    }

    #endregion
}