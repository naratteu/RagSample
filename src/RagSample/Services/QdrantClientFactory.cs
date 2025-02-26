using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using RagSample.Configurations;

namespace RagSample.Services;

public sealed class QdrantClientFactory(
    ILogger<QdrantClientFactory> Logger,
    IConfiguration Configuration,
    ILoggerFactory LoggerFactory
)
{
    public QdrantClient CreateQdrantClient()
    {
        var config = Configuration.GetSection("Qdrant").Get<QdrantConfig>();
        ArgumentNullException.ThrowIfNull(config);

        Logger.LogInformation("Qdrant client: {server_url} (API Key: {api_key})",
            config.ServerUrl, string.IsNullOrWhiteSpace(config.ApiKey) ? "Exists" : "Non-Exists");

        var client = new QdrantClient(
            config.ServerUrl,
            config.ApiKey,
            default,
            LoggerFactory);

        return client;
    }
}
