namespace RagSample.Configurations;

public sealed record class OllamaConfig(
    Uri ServerUrl,
    string EmbeddingModelName);
