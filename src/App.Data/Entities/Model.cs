using System.ComponentModel.DataAnnotations;
using Lazarus.Data.Enums;

namespace Lazarus.Data.Entities;

/// <summary>
/// Represents a language model configuration.
/// </summary>
public class Model
{
    /// <summary>
    /// Gets or sets the unique identifier for this model.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name of the model.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file system path to the model.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of inference engine runner.
    /// </summary>
    [Required]
    public RunnerType RunnerType { get; set; }

    /// <summary>
    /// Gets or sets whether this model is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the JSON-serialized parameters for this model.
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// Gets or sets when this model was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this model was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}