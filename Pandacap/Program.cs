using Azure.Identity;
using DeviantArtFs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Pandacap;
using Pandacap.ActivityPub.Favorites;
using Pandacap.ActivityPub.HttpSignatures.Discovery;
using Pandacap.ActivityPub.HttpSignatures.Validation;
using Pandacap.ActivityPub.Inbox;
using Pandacap.ActivityPub.JsonLd;
using Pandacap.ActivityPub.Outbox;
using Pandacap.ActivityPub.RemoteObjects;
using Pandacap.ActivityPub.Services;
using Pandacap.ATProto.HandleResolution;
using Pandacap.ATProto.Services;
using Pandacap.Audio;
using Pandacap.Configuration;
using Pandacap.Constants;
using Pandacap.Credentials;
using Pandacap.Data;
using Pandacap.Database;
using Pandacap.FeedIngestion;
using Pandacap.Frontend.Feeds;
using Pandacap.FurAffinity;
using Pandacap.Inbox;
using Pandacap.KeyVault;
using Pandacap.Lemmy;
using Pandacap.Notifications;
using Pandacap.PlatformLinks;
using Pandacap.Resolvers;
using Pandacap.UI.Posts;
using Pandacap.VectorSearch;
using Pandacap.VectorSearch.Models;
using Pandacap.Weasyl;
using Pandacap.Weasyl.Scraping;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["CosmosDBAccountEndpoint"] is string cosmosDBAccountEndpoint)
{
    if (builder.Configuration["CosmosDBAccountKey"] is string cosmosDBAccountKey)
    {
        builder.Services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
            cosmosDBAccountEndpoint,
            cosmosDBAccountKey,
            databaseName: "Pandacap"));
    }
    else
    {
        builder.Services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
            cosmosDBAccountEndpoint,
            new DefaultAzureCredential(),
            databaseName: "Pandacap"));
    }
}

builder.Services.AddDbContextFactory<PandacapIdentityDbContext>(options => options.UseInMemoryDatabase(nameof(PandacapIdentityDbContext)));

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(new Uri($"https://{builder.Configuration["StorageAccountHostname"]}"));
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

var authenticationBuilder = builder.Services.AddAuthentication();

if (builder.Configuration["Authentication:Microsoft:ClientId"] is string microsoftId
    && builder.Configuration["Authentication:Microsoft:ClientSecret"] is string microsoftSecret
    && builder.Configuration["Authentication:Microsoft:TenantId"] is string microsoftTenant)
{
    authenticationBuilder
        .AddOpenIdConnect(m =>
        {
            m.Authority = $"https://login.microsoftonline.com/{Uri.EscapeDataString(microsoftTenant)}/";
            m.ClientId = microsoftId;
            m.ClientSecret = microsoftSecret;
            m.ResponseType = OpenIdConnectResponseType.Code;
            m.MapInboundClaims = false;
        });
}

if (builder.Configuration["DeviantArtClientId"] is string deviantArtClientId
    && builder.Configuration["DeviantArtClientSecret"] is string deviantArtClientSecret)
{
    builder.Services.AddSingleton(new DeviantArtApp(
        deviantArtClientId,
        deviantArtClientSecret));

    authenticationBuilder.AddDeviantArt(d =>
    {
        d.Scope.Add("browse");
        d.Scope.Add("message");
        d.Scope.Add("note");
        d.Scope.Add("publish");
        d.Scope.Add("stash");
        d.Scope.Add("user.manage");
        d.ClientId = builder.Configuration["DeviantArtClientId"]!;
        d.ClientSecret = builder.Configuration["DeviantArtClientSecret"]!;
        d.SaveTokens = true;
    });
}

if (builder.Configuration["VectorSearchEmbeddingsEndpoint"] is string embeddingsEndpoint
    && builder.Configuration["VectorSearchSearchEndpoint"] is string searchEndpoint
    && builder.Configuration["VectorSearchIndexName"] is string indexName)
{
    builder.Services.AddSingleton(new VectorSearchConfig(
        EmbeddingsEndpoint: embeddingsEndpoint,
        SearchEndpoint: searchEndpoint,
        IndexName: indexName));
}

builder.Services.AddSingleton(new AllowedExternalUserCollection(
    DeviantArtUsers: builder.Configuration["DeviantArtUsername"] is string du ? [du] : [],
    RedditUsers: builder.Configuration["RedditUsername"] is string ru ? [ru] : []));

DeploymentInformation.ApplicationHostname = builder.Configuration["ApplicationHostname"]
    ?? throw new Exception("ApplicationHostname is not defined");

DeploymentInformation.Username = builder.Configuration["ActivityPubUsername"]
    ?? throw new Exception("ActivityPubUsername is not defined");

builder.Services
    .AddActivityPubFavoritesHandler()
    .AddActivityPubInboxHandler()
    .AddActivityPubKeyFinder()
    .AddActivityPubOutboxServices()
    .AddActivityPubRemoteObjectServices()
    .AddActivityPubServices()
    .AddActivityPubSignatureValidator()
    .AddATProtoHandleResolution()
    .AddATProtoServices()
    .AddAudioServices()
    .AddCredentialProviders()
    .AddDnsClient()
    .AddFeedBuilder()
    .AddFeedReaders()
    .AddFurAffinityClient()
    .AddInboxHandlers()
    .AddJsonLdExpansionService()
    .AddLemmyServices()
    .AddPandacapKeyVault(new()
    {
        KeyVaultHost = new Uri("https://" + builder.Configuration["KeyVaultHostname"])
    })
    .AddPlatformLinkProvider()
    .AddResolvers()
    .AddUIPostProviders()
    .AddVectorSearch()
    .AddWeasylClient(new()
    {
        WeasylProxyHost = new("https://" + builder.Configuration["WeasylProxyHost"])
    })
    .AddWeasylScraper()
    .AddScoped<ActivityPubAddressedPostNotificationHandler>()
    .AddScoped<ActivityPubNotificationHandler>()
    .AddScoped<ActivityPubReplyNotificationHandler>()
    .AddScoped<CompositeNotificationHandler>()
    .AddScoped<ATProtoNotificationHandler>()
    .AddScoped<DeviantArtFeedNotificationHandler>()
    .AddScoped<DeviantArtNoteNotificationHandler>()
    .AddScoped<FurAffinityNoteNotificationHandler>()
    .AddScoped<FurAffinityNotificationHandler>()
    .AddScoped<PostCreator>()
    .AddScoped<ReplyLookup>()
    .AddScoped<SvgRenderer>()
    .AddScoped<TokenUpdater>()
    .AddScoped<Uploader>()
    .AddScoped<WeasylNoteNotificationHandler>()
    .AddScoped<WeasylNotificationHandler>();

builder.Services.AddHttpClient(string.Empty, client =>
    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<PandacapIdentityDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("vectorSearch", opt =>
    {
        opt.PermitLimit = 50;
        opt.Window = TimeSpan.FromDays(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Profile/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Profile}/{action=Index}");

app.UseRateLimiter();

app.MapRazorPages();

app.Run();
