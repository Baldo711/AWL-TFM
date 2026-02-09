namespace AccessWatchLite.Application.Response;

/// <summary>
/// Result of a response action execution.
/// </summary>
public sealed record ResponseResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
    public string? ErrorMessage { get; init; }

    public static ResponseResult Successful(string message, string? details = null)
        => new() { Success = true, Message = message, Details = details };

    public static ResponseResult Failed(string message, string? errorMessage = null)
        => new() { Success = false, Message = message, ErrorMessage = errorMessage };
}
