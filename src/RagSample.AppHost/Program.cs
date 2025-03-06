var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama", port: 11434);
var qdrant = builder.AddQdrant("qdrant", httpPort: 6333, grpcPort: 6334);

var ragsample = builder.AddProject<Projects.RagSample>("ragsample").WithArgs("--mode=Build")
    .WaitFor(ollama).WaitFor(qdrant).WithEnvironment("Qdrant:ApiKey", qdrant.Resource.ApiKeyParameter.Value);

builder.AddProject<Projects.RagSample_Search>("ragsample-search").WaitForCompletion(ragsample)
    .WithEnvironment("Qdrant:ApiKey", qdrant.Resource.ApiKeyParameter.Value);

builder.Build().Run();
