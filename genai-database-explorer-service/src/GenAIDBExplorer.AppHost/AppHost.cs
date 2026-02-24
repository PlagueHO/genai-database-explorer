var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GenAIDBExplorer_Console>("genaidbexplorer-console");
var api = builder.AddProject<Projects.GenAIDBExplorer_Api>("genaidbexplorer-api");

builder.AddViteApp("genaidbexplorer-frontend", "../../../genai-database-explorer-frontend")
    .WithPnpm()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
