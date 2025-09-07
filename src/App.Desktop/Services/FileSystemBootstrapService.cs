using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.Services;

public sealed class FileSystemBootstrapService : IFileSystemBootstrapService
{
    private readonly ILogger<FileSystemBootstrapService> _logger;

    public FileSystemBootstrapService(ILogger<FileSystemBootstrapService> logger)
    {
        _logger = logger;
    }

    public Task EnsureLayoutAsync(CancellationToken cancellationToken = default)
    {
        // Use exactly the directories defined by LazarusPaths. Idempotent: only create if missing.
        // Determine which leaf directories need creation (pre-check), then ensure all
        var dirs = DirectoryBootstrap.LeafDirectories.ToArray();
        var toCreate = dirs.Where(d => !Directory.Exists(d)).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList();
        var already = dirs.Except(toCreate, StringComparer.OrdinalIgnoreCase).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList();

        DirectoryBootstrap.EnsureAll();

        if (toCreate.Count > 0)
        {
            _logger.LogInformation("Created {Count} Lazarus directories:", toCreate.Count);
            foreach (var c in toCreate)
            {
                _logger.LogInformation("  [Created] {Path}", c);
            }
        }
        _logger.LogInformation("Verified {Count} existing Lazarus directories:", already.Count);
        foreach (var e in already)
        {
            _logger.LogInformation("  [Exists ] {Path}", e);
        }

        return Task.CompletedTask;
    }
}
