using Hangfire;
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
using Pandacap.FurAffinity.Interfaces;
using Pandacap.Inbox;
using Pandacap.Ingestion;
using Pandacap.Local;
using Pandacap.OfflineNotifications;
using Pandacap.Outbox;
using Pandacap.PeriodicTasks;
using Pandacap.Rss;
using Pandacap.UI.Posts;
using Pandacap.Weasyl;
using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Scraping;

var builder = WebApplication.CreateBuilder(args);

DeploymentInformation.ApplicationHostname = builder.Configuration["ApplicationHostname"]
    ?? throw new Exception("ApplicationHostname is not defined");

builder.Services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
    builder.Configuration["CosmosDBAccountEndpoint"]!,
    builder.Configuration["CosmosDBAccountKey"]!,
    databaseName: "Pandacap"));

builder.Services
    .AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage())
    .AddHangfireServer()
    .AddHttpClient()
    .AddMemoryCache()
    .AddSingleton(TimeProvider.System)
    .AddSingleton<IFurAffinityCredentials>(new FurAffinityCredentials(
        builder.Configuration["FurAffinityA"]!,
        builder.Configuration["FurAffinityB"]!))
    .AddSingleton<IWeasylCredentials>(new WeasylCredentials(
        builder.Configuration["WeasylApiKey"]!));

if (builder.Configuration["FurAffinityA"] is string a && builder.Configuration["FurAffinityB"] is string b)
    builder.Services.AddSingleton<IFurAffinityCredentials>(new FurAffinityCredentials(a, b));

if (builder.Configuration["WeasylApiKey"] is string weasylApiKey)
    builder.Services.AddSingleton<IWeasylCredentials>(new WeasylCredentials(weasylApiKey));

builder.Services
    .AddATProtoHandleResolution()
    .AddATProtoServices()
    .AddBridgingServices()
    .AddCredentialProviders()
    .AddDeviantArtClient()
    .AddDnsClient()
    .AddFavoritesHandlers()
    .AddFeedReaders()
    .AddFurAffinityClient()
    .AddInboxSources()
    .AddIngestion()
    .AddOfflineNotificationsSources()
    .AddOutboxDestinations()
    .AddPeriodicTaskServices()
    .AddUIPostProviders()
    .AddWeasylClient()
    .AddWeasylScraper();

builder.Services
    .AddTransient<BridgedPostDiscoveryService>()
    .AddTransient<DismissedInboxPostCleanupService>()
    .AddTransient<FavoritesIngestionService>()
    .AddTransient<InboxService>()
    .AddTransient<OfflineNotificationsSynchronizationService>()
    .AddTransient<OfflinePlatformCacheSynchronizationService>()
    .AddTransient<OutboundActivityCleanupService>()
    .AddTransient<OutboundActivityTriggerService>()
    .AddTransient<OutboxService>()
    .AddTransient<UnreadInboxPostCleanupService>();

var app = builder.Build();

app.UseHangfireDashboard(
    pathMatch: "",
    new DashboardOptions
    {
        Authorization = [new DashboardAuthorizationFilter()]
    });

AddRecurringJob<BridgedPostDiscoveryService>(Cron.MinuteInterval(30));
AddRecurringJob<DismissedInboxPostCleanupService>(Cron.Daily());
AddRecurringJob<FavoritesIngestionService>(Cron.HourInterval(4));
AddRecurringJob<InboxService>(Cron.HourInterval(4));
AddRecurringJob<OfflineNotificationsSynchronizationService>(Cron.HourInterval(8));
AddRecurringJob<OfflinePlatformCacheSynchronizationService>(Cron.Weekly());
AddRecurringJob<OutboundActivityCleanupService>(Cron.Daily());
AddRecurringJob<OutboundActivityTriggerService>(Cron.Minutely());
AddRecurringJob<OutboxService>(Cron.MinuteInterval(5));
AddRecurringJob<UnreadInboxPostCleanupService>(Cron.Weekly());

app.Run($"http://+:5002");

static void AddRecurringJob<T>(string cronExpression) where T : IPandacapBackgroundService =>
    RecurringJob.AddOrUpdate<T>(
        recurringJobId: typeof(T).Name,
        svc => svc.RunAsync(),
        cronExpression);

record WeasylCredentials(string WeasylApiKey) : IWeasylCredentials;

record FurAffinityCredentials(string A, string B) : IFurAffinityCredentials
{
    string IFurAffinityCredentials.UserAgent => Pandacap.Constants.UserAgentInformation.UserAgent;
}
