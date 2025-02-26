using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using OllamaSharp.Models;
using Qdrant.Client.Grpc;
using RagSample.Services;

#pragma warning disable SKEXP0050

namespace RagSample.Programs;

public sealed class BuildVectorDatabaseProgram(
    IHostApplicationLifetime Lifetime,
    ILogger<BuildVectorDatabaseProgram> Logger,
    QdrantClientFactory QdrantClientFactory,
    OllamaClientFactory OllamaClientFactory,
    MediaWikiArchiveReader ArchiveReader
) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            using var qdrantClient = QdrantClientFactory.CreateQdrantClient();
            using var ollamaClient = await OllamaClientFactory.CreateOllamaClientAsync(stoppingToken);

            var collectionName = "ragsample";

            var exists = await qdrantClient.CollectionExistsAsync(collectionName, stoppingToken);
            if (!exists)
            {
                var vp = new VectorParams()
                {
                    Size = 1024,
                    Distance = Distance.Cosine,
                    Datatype = Datatype.Float16,
                    OnDisk = false,
                };
                await qdrantClient.RecreateCollectionAsync(
                    collectionName, vp,
                    cancellationToken: stoppingToken)
                    .ConfigureAwait(false);
            }

            var sep = new string('\n', 2);
            await foreach (var eachPageData in ArchiveReader.ReadWikipediaDumpAsync(Array.Empty<string>(), stoppingToken).ConfigureAwait(false))
            {
                if (!eachPageData.Title.Contains("제주", StringComparison.Ordinal))
                    continue;

                if (eachPageData.Title.Contains("분류:") ||
                    eachPageData.Title.Contains("틀:") ||
                    eachPageData.Title.Contains("위키백과:") ||
                    eachPageData.Title.Contains("파일:"))
                    continue;

                var text = eachPageData.Text.Trim();

                if (text.Equals("#넘겨주기") ||
                    text.Equals("#REDIRECT"))
                    continue;

                text = string.Join(" | ",
                    $"문서 제목: {eachPageData.Title}",
                    eachPageData.Text);

                var chunks = TextChunker.SplitPlainTextParagraphs(
                    TextChunker.SplitPlainTextLines(text, 75),
                    150, 50);

                var response = await ollamaClient.EmbedAsync(new EmbedRequest()
                {
                    Input = chunks,
                }, stoppingToken).ConfigureAwait(false);

                if (response.Embeddings.Count < 1)
                    continue;

                var vectors = response.Embeddings.Select((x, i) => new PointStruct
                {
                    Id = Guid.NewGuid(),
                    Vectors = new Vectors() { Vector = x, },
                    Payload =
                    {
                        ["document_id"] = eachPageData.Id,
                        ["document_title"] = eachPageData.Title,
                        ["paragraph_seq"] = i,
                    },
                }).ToList();

                Logger.LogInformation(
                    "Qdrant upsert: {count} vectors, {title} ({id})",
                    vectors.Count, eachPageData.Title, eachPageData.Id);

                var result = await qdrantClient.UpsertAsync(
                    collectionName, vectors, cancellationToken: stoppingToken)
                    .ConfigureAwait(false);

                Logger.LogInformation(
                    "Qdrant upsert status: {status} (Operation ID: {oid})",
                    result.Status, result.OperationId);
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
