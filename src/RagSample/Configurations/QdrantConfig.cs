namespace RagSample.Configurations;

public sealed record class QdrantConfig(
    Uri ServerUrl,
    string? ApiKey = default);
