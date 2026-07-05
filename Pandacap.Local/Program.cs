using Microsoft.EntityFrameworkCore;
using Pandacap.ATProto.HandleResolution;
using Pandacap.ATProto.Services;
using Pandacap.Bridging;
using Pandacap.Configuration;
using Pandacap.Credentials;
using Pandacap.Database;
using Pandacap.DeviantArt;
using Pandacap.Favorites;
using Pandacap.FurAffinity;
using Pandacap.Inbox;
using Pandacap.Local;
using Pandacap.Outbox;
using Pandacap.PeriodicTasks;
using Pandacap.UI.Posts;
using Pandacap.Weasyl;
using Pandacap.Weasyl.Scraping;

var builder = WebApplication.CreateBuilder(args);

DeploymentInformation.ApplicationHostname = builder.Configuration["ApplicationHostname"]
    ?? throw new Exception("ApplicationHostname is not defined");

builder.Services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
    builder.Configuration["CosmosDBAccountEndpoint"]!,
    builder.Configuration["CosmosDBAccountKey"]!,
    databaseName: "Pandacap"));

builder.Services
    .AddHttpClient()
    .AddMemoryCache()
    .AddSingleton(TimeProvider.System);

builder.Services
    .AddATProtoHandleResolution()
    .AddATProtoServices()
    .AddBridgingServices()
    .AddCredentialProviders()
    .AddDeviantArtClient()
    .AddDnsClient()
    .AddFavoritesHandlers()
    .AddFurAffinityClient()
    .AddInboxHandlers()
    .AddOutboxHandlers()
    .AddPeriodicTaskServices()
    .AddUIPostProviders()
    .AddWeasylClient(
        weasylProxyHost: new("https://" + builder.Configuration["WeasylProxyHost"]))
    .AddWeasylScraper();

builder.Services
    .AddHostedService<BridgedPostDiscoveryService>()
    .AddHostedService<DismissedInboxPostCleanupService>()
    .AddHostedService<FavoritesIngestionService>()
    .AddHostedService<FolderSynchronizationService>()
    .AddHostedService<InboxIngestionService>()
    .AddHostedService<OutboundActivityCleanupService>()
    .AddHostedService<OutboundActivityTriggerService>()
    .AddHostedService<UnreadInboxPostCleanupService>();

var app = builder.Build();

app.MapGet("/", () => "Pandacap Local Sidecar");

app.Run($"http://+:5002");
