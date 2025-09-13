using Azure.Identity;
using DeviantArtFs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Pandacap;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.Notifications;
using Pandacap.Podcasts;
using Pandacap.Signatures;

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

string tenantId = builder.Configuration["Authentication:Microsoft:TenantId"]!;

builder.Services.AddAuthentication()
    .AddMicrosoftAccount(m =>
    {
        m.AuthorizationEndpoint = $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId)}/oauth2/v2.0/authorize";
        m.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
        m.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
        m.TokenEndpoint = $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId)}/oauth2/v2.0/token";
    })
    .AddDeviantArt(d =>
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
    })
    .AddReddit(o => {
        o.Scope.Add("read");
        o.Scope.Add("history");
        o.ClientId = builder.Configuration["RedditAppId"]!;
        o.ClientSecret = builder.Configuration["RedditAppSecret"]!;
        o.SaveTokens = true;
    });

builder.Services.AddSingleton(new DeviantArtApp(
    builder.Configuration["DeviantArtClientId"]!,
    builder.Configuration["DeviantArtClientSecret"]!));

builder.Services.AddSingleton(new ComputerVisionConfiguration(
    builder.Configuration["ComputerVisionEndpoint"],
    builder.Configuration["Authentication:Microsoft:TenantId"]));

builder.Services
    .AddPandacapServices(new(
        applicationHostname: builder.Configuration["ApplicationHostname"],
        username: builder.Configuration["ActivityPubUsername"],
        keyVaultHostname: builder.Configuration["KeyVaultHostname"],
        weasylProxyHost: builder.Configuration["WeasylProxyHost"]))
    .AddScoped<ActivityPubNotificationHandler>()
    .AddScoped<ActivityPubRemoteActorService>()
    .AddScoped<ActivityPubRemotePostService>()
    .AddScoped<ActivityPubReplyNotificationHandler>()
    .AddScoped<BlueskyNotificationHandler>()
    .AddScoped<BridgyFedHandleProvider>()
    .AddScoped<CompositeFavoritesProvider>()
    .AddScoped<CompositeNotificationHandler>()
    .AddScoped<ComputerVisionProvider>()
    .AddScoped<DeliveryInboxCollector>()
    .AddScoped<DeviantArtFeedNotificationHandler>()
    .AddScoped<DeviantArtNoteNotificationHandler>()
    .AddScoped<FurAffinityNoteNotificationHandler>()
    .AddScoped<FurAffinityNotificationHandler>()
    .AddScoped<MastodonVerifier>()
    .AddScoped<PostCreator>()
    .AddScoped<RemoteActivityPubPostHandler>()
    .AddScoped<ReplyLookup>()
    .AddScoped<SvgRenderer>()
    .AddScoped<Uploader>()
    .AddScoped<WeasylNoteNotificationHandler>()
    .AddScoped<WeasylNotificationHandler>()
    .AddScoped<WmaZipSplitter>();

builder.Services.AddHttpClient();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<PandacapIdentityDbContext>();
builder.Services.AddControllersWithViews();

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
app.MapRazorPages();

app.Run();
