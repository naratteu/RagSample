using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RagSample.Services;
using RagSample.Programs;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", true);
builder.Configuration.AddCommandLine(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton<QdrantClientFactory>();
builder.Services.AddSingleton<OllamaClientFactory>();
builder.Services.AddSingleton<MediaWikiArchiveReader>();

switch (builder.Configuration["Mode"]?.ToUpperInvariant())
{
	case "BUILD":
        builder.Services.AddHostedService<BuildVectorDatabaseProgram>();
		break;

	case "SEARCH":
        builder.Services.AddHostedService<SearchVectorDatabaseProgram>();
        break;

    default:
		break;
}

var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Selected mode: {mode}", configuration["Mode"]);

app.Run();
