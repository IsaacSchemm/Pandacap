using Azure.Identity;
using DeviantArtFs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pandacap.ActivityPub.JsonLd;
using Pandacap.ActivityPub.RemoteObjects;
using Pandacap.ActivityPub.Services;
using Pandacap.ATProto.Services;
using Pandacap.Bridging;
using Pandacap.Configuration;
using Pandacap.Credentials;
using Pandacap.Database;
using Pandacap.DeviantArt;
using Pandacap.Favorites;
using Pandacap.Inbox.ATProto;
using Pandacap.Inbox.Feeds;
using Pandacap.KeyVault;
using Pandacap.ManualInboxIngestion.ATProto;
using Pandacap.ManualInboxIngestion.Feeds;
using Pandacap.PeriodicTasks;
using Pandacap.Rss;
using Pandacap.UI.Posts;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
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

        if (Environment.GetEnvironmentVariable("DeviantArtClientId") is string deviantArtClientId
            && Environment.GetEnvironmentVariable("DeviantArtClientSecret") is string deviantArtClientSecret)
        {
            services.AddSingleton(new DeviantArtApp(
                deviantArtClientId,
                deviantArtClientSecret));
        }

        DeploymentInformation.ApplicationHostname = Environment.GetEnvironmentVariable("ApplicationHostname")
            ?? throw new Exception("ApplicationHostname is not defined");

        DeploymentInformation.Username = Environment.GetEnvironmentVariable("ActivityPubUsername")
            ?? throw new Exception("ActivityPubUsername is not defined");

        services
            .AddActivityPubOutboundServices()
            .AddActivityPubRemoteObjectServices()
            .AddATProtoFeedRefresher()
            .AddATProtoInboxSources()
            .AddATProtoServices()
            .AddBridgingServices()
            .AddCredentialProviders()
            .AddDeviantArtClient()
            .AddFavoritesHandlers()
            .AddFeedInboxSources()
            .AddFeedReaders()
            .AddFeedRefresher()
            .AddJsonLdExpansionService()
            .AddMemoryCache()
            .AddPandacapKeyVault(
                keyVaultHost: new Uri("https://" + Environment.GetEnvironmentVariable("KeyVaultHostname")))
            .AddPeriodicTaskServices()
            .AddUIPostProviders();

        services
            .AddHttpClient()
            .AddSingleton(TimeProvider.System);
    })
    .Build();

host.Run();
