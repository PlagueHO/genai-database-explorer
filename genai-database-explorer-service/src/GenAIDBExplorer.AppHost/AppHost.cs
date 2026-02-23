var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GenAIDBExplorer_Console>("genaidbexplorer-console");
builder.AddProject<Projects.GenAIDBExplorer_Api>("genaidbexplorer-api");

builder.Build().Run();
