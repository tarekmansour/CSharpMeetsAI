using OllamaSharp.Models;

var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    ;

var ollamaModel = ollama.AddModel("ollama-model", "phi4")
    ;

builder.AddProject<Projects.CSharpMeetsAI_Api>("csharpmeetsai-api")
    .WithReference(ollamaModel)
    .WaitFor(ollamaModel)
    ;

builder.Build().Run();