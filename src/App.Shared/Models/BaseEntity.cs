using System.ComponentModel.DataAnnotations;

namespace Lazarus.App.Shared.Models;

/// <summary>
/// Base entity class providing common properties for all domain entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp when this entity was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this entity was last updated
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entity has been deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; }
}