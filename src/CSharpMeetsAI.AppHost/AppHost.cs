var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CSharpMeetsAI_Api>("csharpmeetsai-api");

builder.Build().Run();
