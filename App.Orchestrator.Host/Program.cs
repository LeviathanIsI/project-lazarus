using Lazarus.Orchestrator;

Console.WriteLine("Starting Lazarus Orchestrator on http://127.0.0.1:11711 ...");
await OrchestratorHost.StartAsync("http://127.0.0.1:11711");
Console.WriteLine("Orchestrator is up. Press Ctrl+C to exit.");
await Task.Delay(Timeout.InfiniteTimeSpan);
