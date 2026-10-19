using Mcp.Worker;

var app = WorkerHost.Create(args);
await app.RunAsync();
