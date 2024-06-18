using Azure.Identity;
using DeviantArtFs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.HighLevel;
using Pandacap.Signatures;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["CosmosDBAccountEndpoint"] is string cosmosDBAccountEndpoint)
{
    if (builder.Configuration["CosmosDBAccountKey"] is string cosmosDBAccountKey)
    {
        builder.Services.AddDbContextFactory<PandacapDbContext>(options => options.UseCosmos(
            cosmosDBAccountEndpoint,
            cosmosDBAccountKey,
            databaseName: "Pandacap"));
        builder.Services.AddDbContext<PandacapDbContext>(options => options.UseCosmos(
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
        builder.Services.AddDbContext<PandacapDbContext>(options => options.UseCosmos(
            cosmosDBAccountEndpoint,
            new DefaultAzureCredential(),
            databaseName: "Pandacap"));
    }
}

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(new Uri($"https://{builder.Configuration["StorageAccountHostname"]}"));
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

if (builder.Configuration["DeviantArtClientId"] is string deviantArtClientId
    && builder.Configuration["DeviantArtClientSecret"] is string deviantArtClientSecret)
{
    builder.Services.AddAuthentication()
        .AddDeviantArt(d =>
        {
            d.Scope.Add("browse");
            d.Scope.Add("message");
            d.ClientId = deviantArtClientId;
            d.ClientSecret = deviantArtClientSecret;
            d.SaveTokens = true;
        });

    builder.Services.AddSingleton(new DeviantArtApp(
        deviantArtClientId,
        deviantArtClientSecret));
}

builder.Services.AddScoped<MastodonVerifier>();

builder.Services.AddPandacapServices(new ApplicationInformation(
    applicationHostname: builder.Configuration["ApplicationHostname"],
    deviantArtUsername: builder.Configuration["DeviantArtUsername"],
    keyVaultHostname: builder.Configuration["KeyVaultHostname"],
    handleHostname: builder.Configuration["ApplicationHostname"],
    storageAccountHostname: builder.Configuration["StorageAccountHostname"],
    webFingerDomains: SetModule.Empty<string>()));

builder.Services.AddHttpClient();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<PandacapDbContext>();
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
    pattern: "{controller=Profile}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
