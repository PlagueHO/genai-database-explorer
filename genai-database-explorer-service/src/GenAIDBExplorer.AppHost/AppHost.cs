var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GenAIDBExplorer_Console>("genaidbexplorer-console");

builder.Build().Run();
