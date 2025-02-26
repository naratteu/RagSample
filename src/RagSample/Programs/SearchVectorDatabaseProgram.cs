using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OllamaSharp.Models;
using RagSample.Services;

namespace RagSample.Programs;

public sealed class SearchVectorDatabaseProgram(
    IHostApplicationLifetime Lifetime,
    ILogger<SearchVectorDatabaseProgram> Logger,
    QdrantClientFactory QdrantClientFactory,
    OllamaClientFactory OllamaClientFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var qdrantClient = QdrantClientFactory.CreateQdrantClient();
            using var ollamaClient = await OllamaClientFactory.CreateOllamaClientAsync(stoppingToken);

            var collectionName = "ragsample";

            var exists = await qdrantClient.CollectionExistsAsync(collectionName, stoppingToken);
            if (!exists)
                throw new Exception($"{collectionName} collection does not exist.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Console.Out.WriteAsync("Type a question to search (Press enter to stop this application): ").ConfigureAwait(false);
                var query = await Console.In.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(query))
                    break;

                var response = await ollamaClient.EmbedAsync(new EmbedRequest()
                {
                    Input = new string[] { query, }.ToList(),
                }, stoppingToken).ConfigureAwait(false);

                if (response.Embeddings.Count < 1)
                    continue;

                var results = await qdrantClient.SearchAsync(
                    collectionName, response.Embeddings[0],
                    cancellationToken: stoppingToken)
                    .ConfigureAwait(false);

                if (results.Any())
                {
                    var docIdList = new List<string>();

                    foreach (var eachResult in results)
                    {
                        eachResult.Payload.TryGetValue("document_title", out var title);
                        eachResult.Payload.TryGetValue("document_id", out var id);

                        await Console.Out.WriteLineAsync($"* (Score - {eachResult.Score}) Document: {title} ({id}) matched.").ConfigureAwait(false);
                        docIdList.Add(id.StringValue);
                    }

                    //await foreach (var eachFoundDoc in ArchiveReader.ReadWikipediaDumpAsync(docIdList, stoppingToken).ConfigureAwait(false))
                    //{
                    //    await Console.Out.WriteLineAsync($"* Document: {eachFoundDoc.Title} ({eachFoundDoc.Id})").ConfigureAwait(false);
                    //    await Console.Out.WriteLineAsync(eachFoundDoc.Text.Trim()).ConfigureAwait(false);
                    //}
                }
                else
                {
                    await Console.Out.WriteLineAsync("* No matched document.").ConfigureAwait(false);
                }
            }

            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error occurred.");
            Environment.ExitCode = 1;
        }
        finally
        {
            Lifetime.StopApplication();
        }
    }
}
