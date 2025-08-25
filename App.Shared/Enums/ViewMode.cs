namespace App.Shared.Enums;

/// <summary>
/// Defines the complexity level of the UI presentation
/// </summary>
public enum ViewMode
{
    /// <summary>
    /// "I just want pretty pictures" - Minimal UI, essential controls only
    /// </summary>
    Novice,

    /// <summary>
    /// "Show me useful controls" - Balanced UI with advanced options visible
    /// </summary>
    Enthusiast,

    /// <summary>
    /// "Give me all the technical chaos" - Full developer mode with debug info
    /// </summary>
    Developer
}