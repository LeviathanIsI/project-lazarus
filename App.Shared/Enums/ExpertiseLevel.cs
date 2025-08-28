namespace App.Shared.Enums;

/// <summary>
/// Defines user expertise levels for cognitive load analysis and progressive disclosure
/// </summary>
public enum ExpertiseLevel
{
    /// <summary>
    /// New to the domain, needs guidance and simplified interfaces
    /// </summary>
    Beginner = 1,
    
    /// <summary>
    /// Some experience, comfortable with standard features
    /// </summary>
    Intermediate = 2,
    
    /// <summary>
    /// Experienced user, comfortable with advanced features
    /// </summary>
    Advanced = 3,
    
    /// <summary>
    /// Expert level, comfortable with all technical complexity
    /// </summary>
    Expert = 4
}