using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lazarus.Data.Seeds;

/// <summary>
/// Provides default seed data for the Lazarus database.
/// </summary>
public static class DefaultSeedData
{
    /// <summary>
    /// Seeds the database with default data if it doesn't already exist.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedAsync(LazarusDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await SeedDefaultModelsAsync(context, cancellationToken).ConfigureAwait(false);
        await SeedDefaultSettingsAsync(context, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds default model configurations.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedDefaultModelsAsync(LazarusDbContext context, CancellationToken cancellationToken = default)
    {
        // Only seed if no models exist
        if (await context.Models.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var defaultModels = new[]
        {
            new Model
            {
                Id = Guid.NewGuid(),
                Name = "Llama-3.1-8B-Instruct",
                Path = "models/llama-3.1-8b-instruct.gguf",
                RunnerType = RunnerType.LlamaCpp,
                IsActive = true,
                Parameters = """
                {
                  "temperature": 0.7,
                  "max_tokens": 4096,
                  "top_p": 0.9,
                  "repetition_penalty": 1.1,
                  "stop": ["<|eot_id|>"]
                }
                """,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Model
            {
                Id = Guid.NewGuid(),
                Name = "Mistral-7B-Instruct",
                Path = "models/mistral-7b-instruct.gguf",
                RunnerType = RunnerType.LlamaCpp,
                IsActive = false,
                Parameters = """
                {
                  "temperature": 0.7,
                  "max_tokens": 4096,
                  "top_p": 0.9,
                  "repetition_penalty": 1.1
                }
                """,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Model
            {
                Id = Guid.NewGuid(),
                Name = "CodeLlama-13B-Instruct",
                Path = "models/codellama-13b-instruct.gguf",
                RunnerType = RunnerType.LlamaCpp,
                IsActive = false,
                Parameters = """
                {
                  "temperature": 0.2,
                  "max_tokens": 4096,
                  "top_p": 0.95,
                  "repetition_penalty": 1.05
                }
                """,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            }
        };

        context.Models.AddRange(defaultModels);
    }

    /// <summary>
    /// Seeds default application settings.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedDefaultSettingsAsync(LazarusDbContext context, CancellationToken cancellationToken = default)
    {
        // Only seed if no settings exist
        if (await context.Settings.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var defaultSettings = new[]
        {
            new Settings
            {
                Key = "App.Theme",
                Value = "Dark",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "App.AutoSave",
                Value = "true",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "App.MaxConversationHistory",
                Value = "1000",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "Runner.DefaultPort",
                Value = "8080",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "Runner.HealthCheckInterval",
                Value = "10000",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "Runner.StartupTimeout",
                Value = "30000",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "UI.DefaultWindowWidth",
                Value = "1200",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "UI.DefaultWindowHeight",
                Value = "800",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "Chat.AutoScroll",
                Value = "true",
                LastModified = DateTime.UtcNow
            },
            new Settings
            {
                Key = "Chat.ShowTimestamps",
                Value = "true",
                LastModified = DateTime.UtcNow
            }
        };

        context.Settings.AddRange(defaultSettings);
    }
}