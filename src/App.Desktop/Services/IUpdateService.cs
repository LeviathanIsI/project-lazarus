using System;

namespace Lazarus.Desktop.Services;

public sealed record UpdateCheckResult(bool IsAvailable, Version Current, Version? Latest, string? FeedUrl, string? ReleaseNotesUrl, string? Message);

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

