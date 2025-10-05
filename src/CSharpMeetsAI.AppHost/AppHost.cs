var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var ollamaModel = ollama.AddModel("ollama-model", "phi3:mini");

builder.AddProject<Projects.CSharpMeetsAI_Api>("csharpmeetsai-api")
    .WithReference(ollama)
    .WaitFor(ollama)
    .WithReference(ollamaModel)
    .WaitFor(ollamaModel);

builder.Build().Run();