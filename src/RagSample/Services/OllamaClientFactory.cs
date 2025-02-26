using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using RagSample.Configurations;

namespace RagSample.Services;

public sealed class OllamaClientFactory(
    ILogger<OllamaClientFactory> Logger,
    IConfiguration Configuration
)
{
    public async Task<OllamaApiClient> CreateOllamaClientAsync(
        CancellationToken cancellationToken = default)
    {
        var config = Configuration.GetSection("Ollama").Get<OllamaConfig>();
        ArgumentNullException.ThrowIfNull(config);

        Logger.LogInformation("Ollama client: {server_url}",
            config.ServerUrl);

        var client = new OllamaApiClient(config.ServerUrl, config.EmbeddingModelName);

        if (!string.IsNullOrWhiteSpace(config.EmbeddingModelName))
        {
            await foreach (var state in client.PullModelAsync(config.EmbeddingModelName, cancellationToken).ConfigureAwait(false))
            {
                if (state != null)
                    Logger.LogInformation("state: {state}, progress: {progress}", state.Status, state.Percent);
            }
        }

        return client;
    }
}
