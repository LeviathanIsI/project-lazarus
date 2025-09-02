namespace Lazarus.App.Shared.DTOs;

/// <summary>
/// Generic API response wrapper for consistent response formatting
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error code if the operation failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the response
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Creates a successful API response with data
    /// </summary>
    /// <param name="data">The response data</param>
    /// <param name="metadata">Optional metadata</param>
    /// <returns>A successful API response</returns>
    public static ApiResponse<T> SuccessResult(T data, Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failed API response with error information
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="errorCode">The error code</param>
    /// <returns>A failed API response</returns>
    public static ApiResponse<T> ErrorResult(string errorMessage, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Non-generic API response for operations that don't return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful API response without data
    /// </summary>
    /// <param name="metadata">Optional metadata</param>
    /// <returns>A successful API response</returns>
    public static ApiResponse SuccessResult(Dictionary<string, object>? metadata = null)
    {
        return new ApiResponse
        {
            Success = true,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a failed API response with error information
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="errorCode">The error code</param>
    /// <returns>A failed API response</returns>
    public static new ApiResponse ErrorResult(string errorMessage, string? errorCode = null)
    {
        return new ApiResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}