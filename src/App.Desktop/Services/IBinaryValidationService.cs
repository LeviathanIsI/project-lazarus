namespace Lazarus.Desktop.Services;

/// <summary>
/// Service for validating binary availability during application startup.
/// Performs lightweight checks without spawning processes or heavy initialization.
/// </summary>
public interface IBinaryValidationService
{
    /// <summary>
    /// Gets the current binary validation status.
    /// </summary>
    BinaryValidationStatus Status { get; }

    /// <summary>
    /// Validates all required binaries and system compatibility.
    /// Performs file system checks and driver availability validation only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the validation operation.</returns>
    Task ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when validation status changes.
    /// </summary>
    event EventHandler<BinaryValidationStatusChangedEventArgs>? StatusChanged;
}

/// <summary>
/// Represents the current status of binary validation.
/// </summary>
public sealed class BinaryValidationStatus
{
    /// <summary>
    /// Whether all required binaries are available.
    /// </summary>
    public bool BinariesAvailable { get; init; }

    /// <summary>
    /// Whether CUDA driver is available (if checking enabled).
    /// </summary>
    public bool CudaDriverAvailable { get; init; }

    /// <summary>
    /// Path to the llama server executable if found.
    /// </summary>
    public string? LlamaServerPath { get; init; }

    /// <summary>
    /// List of validation issues encountered.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether the system is ready for runner operations.
    /// </summary>
    public bool IsSystemReady => BinariesAvailable && (CudaDriverAvailable || !RequiresCuda);

    /// <summary>
    /// Whether CUDA is required for operations.
    /// </summary>
    public bool RequiresCuda { get; init; }

    /// <summary>
    /// Timestamp when validation was last performed.
    /// </summary>
    public DateTimeOffset LastValidated { get; init; } = DateTimeOffset.Now;
}

/// <summary>
/// Event arguments for binary validation status changes.
/// </summary>
public sealed class BinaryValidationStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// The new validation status.
    /// </summary>
    public BinaryValidationStatus Status { get; }

    public BinaryValidationStatusChangedEventArgs(BinaryValidationStatus status)
    {
        Status = status;
    }
}