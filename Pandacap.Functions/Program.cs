using Azure.Identity;
using DeviantArtFs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pandacap.ActivityPub.JsonLd;
using Pandacap.ActivityPub.Outbox;
using Pandacap.ActivityPub.RemoteObjects;
using Pandacap.ActivityPub.Services;
using Pandacap.ATProto.Services;
using Pandacap.Configuration;
using Pandacap.Credentials;
using Pandacap.Database;
using Pandacap.Favorites;
using Pandacap.FeedIngestion;
using Pandacap.FurAffinity;
using Pandacap.Inbox;
using Pandacap.KeyVault;
using Pandacap.UI.Posts;
using Pandacap.Weasyl;
using Pandacap.Weasyl.Scraping;

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
            .AddActivityPubServices()
            .AddActivityPubOutboxServices()
            .AddActivityPubRemoteObjectServices()
            .AddATProtoServices()
            .AddCredentialProviders()
            .AddFavoritesHandlers()
            .AddFeedReaders()
            .AddFurAffinityClient()
            .AddInboxHandlers()
            .AddJsonLdExpansionService()
            .AddMemoryCache()
            .AddPandacapKeyVault(
                keyVaultHost: new Uri("https://" + Environment.GetEnvironmentVariable("KeyVaultHostname")))
            .AddUIPostProviders()
            .AddWeasylClient(
                weasylProxyHost: new("https://" + Environment.GetEnvironmentVariable("WeasylProxyHost")))
            .AddWeasylScraper();

        services.AddHttpClient();
    })
    .Build();

host.Run();
