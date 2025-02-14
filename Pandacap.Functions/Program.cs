using Azure.Identity;
using DeviantArtFs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Functions.ActivityPub;
using Pandacap.Functions.InboxHandlers;
using Pandacap.HighLevel;
using Pandacap.Clients;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        if (Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint") is string cosmosDBAccountEndpoint)
        {
            if (Environment.GetEnvironmentVariable("CosmosDBAccountKey") is string cosmosDBAccountKey)
            {
                services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
                    cosmosDBAccountEndpoint,
                    cosmosDBAccountKey,
                    databaseName: "Pandacap"));
                services.AddDbContext<PandacapDbContext>(options => options.UseCosmos(
                    cosmosDBAccountEndpoint,
                    cosmosDBAccountKey,
                    databaseName: "Pandacap"));
            }
            else
            {
                services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
                    cosmosDBAccountEndpoint,
                    new DefaultAzureCredential(),
                    databaseName: "Pandacap"));
                services.AddDbContext<PandacapDbContext>(options => options.UseCosmos(
                    cosmosDBAccountEndpoint,
                    new DefaultAzureCredential(),
                    databaseName: "Pandacap"));
            }
        }

        if (Environment.GetEnvironmentVariable("DeviantArtClientId") is string deviantArtClientId
            && Environment.GetEnvironmentVariable("DeviantArtClientSecret") is string deviantArtClientSecret)
        {
            services.AddSingleton(new DeviantArtApp(
                deviantArtClientId,
                deviantArtClientSecret));
        }

        services
            .AddPandacapServices(new(
                applicationHostname: Environment.GetEnvironmentVariable("ApplicationHostname"),
                username: Environment.GetEnvironmentVariable("ActivityPubUsername"),
                keyVaultHostname: Environment.GetEnvironmentVariable("KeyVaultHostname"),
                handleHostname: Environment.GetEnvironmentVariable("ApplicationHostname"),
                weasylProxyHost: Environment.GetEnvironmentVariable("WeasylProxyHost")))
            .AddScoped<ATProtoInboxHandler>()
            .AddScoped<DeviantArtInboxHandler>()
            .AddScoped<FurAffinityInboxHandler>()
            .AddScoped<OutboxProcessor>()
            .AddScoped<WeasylInboxHandler>();

        services.AddHttpClient();
    })
    .Build();

host.Run();
