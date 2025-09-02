using Lazarus.App.Orchestrator.Host.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lazarus.App.Orchestrator.Host;

/// <summary>
/// Entry point for the Lazarus Orchestrator Host application
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting Lazarus Orchestrator Host");

            await host.RunAsync();
            
            logger.LogInformation("Lazarus Orchestrator Host stopped");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host terminated unexpectedly: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Creates the host builder with configured services
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                    optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                });

                // Add hosted services
                services.AddHostedService<OrchestratorHostedService>();

                // Add configuration options
                services.Configure<OrchestratorHostOptions>(
                    context.Configuration.GetSection(OrchestratorHostOptions.SectionName));
            })
            .UseConsoleLifetime();
}