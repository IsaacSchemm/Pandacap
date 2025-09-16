using Azure.Identity;
using DeviantArtFs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pandacap.Clients.ATProto;
using Pandacap.Data;
using Pandacap.Functions;
using Pandacap.Functions.ActivityPub;
using Pandacap.Functions.FavoriteHandlers;
using Pandacap.Functions.InboxHandlers;
using Pandacap.HighLevel;

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
            }
            else
            {
                services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
                    cosmosDBAccountEndpoint,
                    new DefaultAzureCredential(),
                    databaseName: "Pandacap"));
            }
        }

        if (Environment.GetEnvironmentVariable("ConstellationHost") is string constellationHost)
            services.AddSingleton(new ConstellationHost(constellationHost));

        if (Environment.GetEnvironmentVariable("DeviantArtClientId") is string deviantArtClientId
            && Environment.GetEnvironmentVariable("DeviantArtClientSecret") is string deviantArtClientSecret)
        {
            services.AddSingleton(new DeviantArtApp(
                deviantArtClientId,
                deviantArtClientSecret));
        }

        if (Environment.GetEnvironmentVariable("RedditAppId") is string redditAppId
            && Environment.GetEnvironmentVariable("RedditAppSecret") is string redditAppSecret)
        {
            services.AddSingleton(new RedditAppInformation(
                redditAppId,
                redditAppSecret));
        }

        services
            .AddPandacapServices(new(
                applicationHostname: Environment.GetEnvironmentVariable("ApplicationHostname"),
                username: Environment.GetEnvironmentVariable("ActivityPubUsername"),
                keyVaultHostname: Environment.GetEnvironmentVariable("KeyVaultHostname"),
                weasylProxyHost: Environment.GetEnvironmentVariable("WeasylProxyHost")))
            .AddScoped<DeviantArtFavoriteHandler>()
            .AddScoped<DeviantArtInboxHandler>()
            .AddScoped<FurAffinityFavoriteHandler>()
            .AddScoped<FurAffinityInboxHandler>()
            .AddScoped<FurryNetworkFavoriteHandler>()
            .AddScoped<OutboxProcessor>()
            .AddScoped<RedditFavoriteHandler>()
            .AddScoped<SheezyArtFavoriteHandler>()
            .AddScoped<WeasylFavoriteHandler>()
            .AddScoped<WeasylInboxHandler>();

        services.AddHttpClient();
    })
    .Build();

host.Run();
