using Azure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Pandacap.Data;
using Pandacap.LowLevel;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("PandacapDbContextConnection") ?? throw new InvalidOperationException("Connection string 'PandacapDbContextConnection' not found.");

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
}

builder.Services.AddSingleton(new ApplicationInformation(
    applicationHostname: "https://pandacap.example.com",
    deviantArtUsername: builder.Configuration["DeviantArtUsername"],
    handleHostname: "example.org",
    webFingerDomains: SetModule.Empty<string>()));

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
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
