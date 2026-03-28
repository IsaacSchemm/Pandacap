using Azure.Identity;
using DeviantArtFs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Pandacap;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.Signatures;
using Pandacap.ActivityPub.Signatures.Interfaces;
using Pandacap.ActivityPub.SignatureValidation;
using Pandacap.ActivityPub.SignatureValidation.Interfaces;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.VectorSearch;
using Pandacap.Notifications;
using Pandacap.Podcasts;
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

if (builder.Configuration["RedditAppId"] is string redditAppId
    && builder.Configuration["RedditAppSecret"] is string redditAppSecret)
{
    authenticationBuilder.AddReddit(o => {
        o.Scope.Add("read");
        o.Scope.Add("history");
        o.ClientId = builder.Configuration["RedditAppId"]!;
        o.ClientSecret = builder.Configuration["RedditAppSecret"]!;
        o.SaveTokens = true;
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

builder.Services.AddSingleton(new ConstellationHost(
    builder.Configuration["ConstellationHost"]));

builder.Services.AddSingleton(new AllowedExternalUserCollection(
    DeviantArtUsers: builder.Configuration["DeviantArtUsername"] is string du ? [du] : [],
    RedditUsers: builder.Configuration["RedditUsername"] is string ru ? [ru] : []));

builder.Services
    .AddPandacapServices(new(
        applicationHostname: builder.Configuration["ApplicationHostname"],
        username: builder.Configuration["ActivityPubUsername"],
        keyVaultHostname: builder.Configuration["KeyVaultHostname"],
        weasylProxyHost: builder.Configuration["WeasylProxyHost"]))
    .AddActivityPubKeyAcquisition()
    .AddActivityPubKeyVerification()
    .AddScoped<ActivityPubAddressedPostNotificationHandler>()
    .AddScoped<ActivityPubNotificationHandler>()
    .AddScoped<ActivityPubReplyNotificationHandler>()
    .AddScoped<CompositeFavoritesProvider>()
    .AddScoped<CompositeNotificationHandler>()
    .AddScoped<ATProtoNotificationHandler>()
    .AddScoped<DeliveryInboxCollector>()
    .AddScoped<DeviantArtFeedNotificationHandler>()
    .AddScoped<DeviantArtNoteNotificationHandler>()
    .AddScoped<FurAffinityNoteNotificationHandler>()
    .AddScoped<FurAffinityNotificationHandler>()
    .AddScoped<PostCreator>()
    .AddScoped<RemoteActivityPubPostHandler>()
    .AddScoped<ReplyLookup>()
    .AddScoped<SvgRenderer>()
    .AddScoped<TokenUpdater>()
    .AddScoped<Uploader>()
    .AddScoped<WeasylNoteNotificationHandler>()
    .AddScoped<WeasylNotificationHandler>()
    .AddScoped<WmaZipSplitter>();

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
