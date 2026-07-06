using Microsoft.EntityFrameworkCore;
using Pandacap.ATProto.HandleResolution;
using Pandacap.ATProto.Services;
using Pandacap.Bridging;
using Pandacap.Configuration;
using Pandacap.Credentials;
using Pandacap.Database;
using Pandacap.DeviantArt;
using Pandacap.Favorites;
using Pandacap.FeedIngestion;
using Pandacap.FurAffinity;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.Inbox.ATProto;
using Pandacap.Inbox.Feeds;
using Pandacap.Inbox.Other;
using Pandacap.Local;
using Pandacap.ManualInboxIngestion.ATProto;
using Pandacap.ManualInboxIngestion.Feeds;
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
    .AddSingleton(TimeProvider.System)
    .AddSingleton<IFurAffinityCredentials>(new FurAffinityCredentials(
        builder.Configuration["FurAffinityA"]!,
        builder.Configuration["FurAffinityB"]!));

builder.Services
    .AddATProtoFeedRefresher()
    .AddATProtoHandleResolution()
    .AddATProtoInboxSources()
    .AddATProtoServices()
    .AddBridgingServices()
    .AddCredentialProviders()
    .AddDeviantArtClient()
    .AddDnsClient()
    .AddFavoritesHandlers()
    .AddFeedInboxSources()
    .AddFeedReaders()
    .AddFeedRefresher()
    .AddFurAffinityClient()
    .AddOtherInboxSources()
    .AddOutboxDestinations()
    .AddPeriodicTaskServices()
    .AddUIPostProviders()
    .AddWeasylClient(
        weasylApiKey: new(builder.Configuration["WeasylApiKey"]),
        weasylProxyHost: new("https://" + builder.Configuration["WeasylProxyHost"]))
    .AddWeasylScraper();

builder.Services
    .AddHostedService<BridgedPostDiscoveryService>()
    .AddHostedService<DismissedInboxPostCleanupService>()
    .AddHostedService<FavoritesIngestionService>()
    .AddHostedService<InboxService>()
    .AddHostedService<OfflinePlatformCacheSynchronizationService>()
    .AddHostedService<OutboundActivityCleanupService>()
    .AddHostedService<OutboundActivityTriggerService>()
    .AddHostedService<OutboxService>()
    .AddHostedService<UnreadInboxPostCleanupService>();

var app = builder.Build();

app.MapGet("/", () => "Pandacap Local Sidecar");

app.Run($"http://+:5002");

record FurAffinityCredentials(string A, string B) : IFurAffinityCredentials
{
    string IFurAffinityCredentials.UserAgent => Pandacap.Constants.UserAgentInformation.UserAgent;
}
