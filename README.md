# Pandacap

A single-user art gallery, feed reader, and social media server, built using F# and C#.

On the home page:

* Your avatar and username
* Links to your attached accounts
* Your ActivityPub handle
* Links to users and feeds you follow
* Up to 8 of your **image posts**
* Up to 12 image posts from your **favorites**
* Up to 5 of your **text posts**

Features:

* Create **image posts** and **text posts**, which are available on the site, via RSS/Atom, and via ActivityPub
* Crosspost your image posts and text posts to attached DeviantArt, Fur Affinity, or Weasyl accounts
* View posts from users or feeds you follow in the **inbox**, split among **image posts**, **text posts**, **shares**, and **podcasts**, and grouped by author
    * Non-ActivityPub posts are periodically imported (~3 times per day)
* View **notifications** from activity on your posts or from your attached accounts
* Add posts to your **favorites** or import them from attached accounts

Some of the things Pandacap does **not** do:

* Act as an OAuth server.
* Act as, or allow the user to directly post to, an atproto PDS.
* Host more than one user account.
* Host any public-facing content that is not intentionally placed there by the user.
* Create posts with more than one attached image.
* Attach images to replies.
* Let you "repost" / "boost" someone else's post.

## Techincal Information

Pandacap is a single-user application.
To log in, the instance owner must use a Microsoft account that they have explicitly allowed in the associated Entra ID app registration.

> **Any authenticated user can access the same data**.
> This means authorization is the sole reponsibility of your Entra ID registration, so only one user account should be allowed access.

Supported protocols and platforms:

### ActivityPub

Pandacap acts as an ActivityPub S2S server, hosting a single actor.

Most public posts are sent to followers via ActivityPub.
Journal entries are federated using the `Article` type; artwork and status updates use `Note`. (Scraps are not federated.)
Pandacap also has the concept of an "addressed post", an unlisted message with specific recipients; these are used for ActivityPub replies and for top-level posts to Lemmy communities.

Pandacap allows you to follow ActivityPub users. This is implemented in the typical way, with `Follow` activites and an inbox path at `/ActivityPub/Inbox`.
Posts from users you follow are sent to the appropriate section of the Pandacap inbox, and you can choose to ignore boosts or to treat all posts from the user as text posts.

When you click on an ActivityPub post as a logged-in user, Pandacap will always fetch the post from its original instance.

Activities (such as `Like`, `Dislike`, `Announce`) and replies to your posts are shown in the Notifications section. (Mentions that are not replies go to the Pandacap inbox.)
Adding a post to your Favorites is equivalent to a `Like`.

### ATProto

#### Following

Pandacap allows you to follow atproto accounts as feeds.
For each user, you can choose whether to follow Bluesky posts, reposts, and/or likes.

Pandacap will also look for Bluesky profile data (name and icon) when it refreshes the feed (every 8 hours, just like for RSS feeds).

If you view a Bluesky post while logged in, and Pandacap detects that the post is available via Bridgy Fed, it will show the ActivityPub version of the post, to allow you to like and reply to it.

All data is fetched (unauthenticated) from the individual user's PDS, using `com.atproto.repo.listRecords`; the Bluesky AppView is not used.

Bluesky's CDN is used for thumbnails and avatars.

#### Posting

Pandacap allows you to enable and disable Bridgy Fed from the profile page while logged in.
You can also add a separate atproto account from the profile page and crosspost to it (as is done with DeviantArt, etc.)

Pandacap uses [Constellation](https://github.com/at-microcosm/microcosm-rs/tree/main/constellation) to find atproto links back to your bridged posts.
Once per hour, if you've posted a new post (and have Bridgy Fed enabled), Pandacap will find the bridged version of your post and add a "View on Bluesky" link to the post's page.

#### Notifications

When you view the Notifications page, Pandacap will use Constellation to populate it with:

* Mentions
* Follows
* For posts you've made within the past 30 days that have a "View on Bluesky" link:
    * Likes
    * Reposts
    * Replies

### DeviantArt / Fur Affinity / Weasyl

Pandacap interacts with these platforms through a combination of scraping and APIs, and requires valid account credentials.
You can crosspost to any or all of these platforms and view their notifications in the Notifications section.

Posts from users you follow on these platforms will appear in the Pandacap inbox, and posts you add to your favorites on these platforms will appear in Pandacap's Favorites automatically.

Fur Affinity support relies on [FAExport](https://faexport.spangle.org.uk/) for most functions, and Weasyl support relies on a PHP proxy script (included in this repository).

### Reddit

Pandacap can connect to a Reddit account, to monitor it for upvotes on top-level posts and add these posts to your Favorites.

### RSS / Atom

Pandacap can follow RSS and Atom feeds. New posts are added to the appropriate section of the Pandacap inbox.

Posts with `audio/mpeg` attachments are sent to the Podcasts section, where you can download the file or play it in a pop-up window.

Pandacap also makes your own posts available over RSS and Atom; the Gallery and Text Posts pages have links to these feeds.

## Software Architecture

Deployable applications:

* **Pandacap**: The main ASP.NET Core project. Hosts public content (artwork, status updates, journals) and private content (e.g. inbox and notification pages).
* **Pandacap.Functions**: Runs periodic tasks (see below for more details).

Libraries:

* **Pandacap.LowLevel**
    * **ConfigurationObjects**: Contains objects that store deployment-level data (i.e. hostname, username) and codebase-level data (e.g. software name, public website).
    * **PlatformBadges**: Contains types that represent the platforms supported by Pandacap and corresponding displayable badges for the UI.
    * **Html**:  Parses and scrapes data from HTML pages.
    * **FurAffinity**: Connects to FurAffinity and FAExport.
    * **ActivityPub**: Creates objects representing posts, favorites, the user profile, etc., which can be sent to, or retrieved by, other servers via ActivityPub.
        * **Communication**: Sends and retrieves objects to/from remote servers via ActivityPub.
        * **Inbound**: Parses objects recieved or retrieved via ActivityPub (posts and actors), converting them into an abstracted form.
    * **ATProto**: Contains atproto client code and abstractions.
    * **Podcasts**: Code for splitting and re-encoding podcasts for transfer to an audio CD.
    * **Data**: Contains the EF Core data models that are used in the Cosmos DB database to store the user's data.
    * **Clients**: Contains miscellaneous small API clients.
* **Pandacap.HighLevel**: Contains shared Pandacap code, including RSS/Atom feed support and code to assemble the user's notifications and favorites into composite lists.

## Deployment

This application runs on the following Azure resources:

* A Cosmos DB NoSQL database (for data storage)
* An Azure Functions app
* A web app
* A Key Vault
* A blob storage account

ASP.NET Core Identity is backed by an in-memory database (since 11.1.0); the only allowed login method is via Microsoft account, but DeviantArt and Reddit accounts can be added in user management (which will connect these accounts to Pandacap's main database).

The web app and function app must have the appropriate IAM permissions to access the storage account (Storage Blob Data Contributor) and the key vault (Key Vault Crypto User).

Function app responsibilities:

* `BridgedPostDiscovery`
    * populates likes back to Bluesky for your posts from the past 2 days
* `FavoriteIngest`
    * check accounts for new favorites / likes / upvotes
* `InboxCleanup`
    * delete any dismissed inbox entries more than 7 days old
* `LongTermInboxCleanup`
    * automatically dismiss active inbox entries (the 200 most recent posts, and any other posts newer than 30 days, will be kept)
* `InboxIngest`
    * check feeds for new posts
* `OutboxCleanup`
    * remove unsent outbound ActivityPub messages that have been pending for more than 7 days
* `SendOutbound`
    * attempt to send any pending outbound ActivityPub messages (if a failure occurs, the recipient will be skipped for the next hour)

### Authorization

This version of Pandacap uses Entra ID as the primary authentication and authorization method. To set up:

* Create a new **app registration** in your Entra directory in the Azure portal.
    * "Who can use": select "Accounts in this organizational directory only"
    * For the redirect URI, choose "Web" and type `https://localhost:7206/signin-oidc`
        * For production, also add the equivalent production URL to this list
* Go to the **app registrations** section of your Entra directory and find the app there.
    * Get the client (application) ID from the main page here - it should be a GUID
    * Click **endpoints** on the horizontal bar to get:
        * "OAuth 2.0 authorization endpoint (v2)" (`AuthorizationEndpoint`)
        * "OAuth 2.0 token endpoint (v2)" (`TokenEndpoint`)
    * Under **certificates and secrets**, add a new client secret. This will be added to the application settings.
* Go to the **enterprise applications** section of your Entra directory and find the app there.
    * Set **assignment required** to "Yes" in the "properties" section.
    * Add your own user account in the "users and groups" section.

### Configuration

Application settings (for both the function app and the web app):

| Name                    | Purpose
| ----------------------- | -----------------------------------------------------
| ActivityPubUsername     | Username to use for ActivityPub and on the home page
| ApplicationHostname     | Public hostname of the app
| ConstellationHost       | URL of a Constellation server for adding ATProto activity to the notifications page
| CosmosDBAccountEndpoint | URL of the database
| CosmosDBAccountKey      | Database key
| DeviantArtClientId      | OAuth client ID from DeviantArt
| DeviantArtClientSecret  | OAuth secret from DeviantArt
| RedditAppId             | OAuth client ID from Reddit
| RedditAppSecret         | OAuth secret from Reddit
| KeyVaultHostname        | Key vault hostname
| StorageAccountHostname  | Hostname of the Azure blob storage account used for storing images associated with your posts or avatar
| WeasylProxyHost         | Hostname that has `/pandacap/weasyl_proxy.php` and `/pandacap/weasyl_submit.php`

Application settings (for the web app only):

| Name                                  | Purpose
| ------------------------------------- | -----------------------------------------------------------------------
| Authentication:Microsoft:TenantId     | Tenant ID of your Entra (AAD) directory
| Authentication:Microsoft:ClientId     | Application (client) ID of the app registration you've created in Entra
| Authentication:Microsoft:ClientSecret | A client secret generated for the app registration

The CosmosDBAccountKey is optional; without it, Pandacap will try to connect
to Cosmos DB using Entra authentication, which can lead to slower performance.
In that case, run something like this in Azure CLI (PowerShell) to give the
appropriate permissions to the function app's managed identity:

    az login
    az cosmosdb sql role assignment create --account-name {...} --resource-group {...} --scope "/" --principal-id {...} --role-definition-id 00000000-0000-0000-0000-000000000002

The `{...}` for the principal ID should be the ID shown in the Identity tab of
the function app settings.

---

Web app user secrets example:

    {
      "ApplicationHostname": "example.azurewebsites.net",
      "Authentication": {
        "Microsoft": {
          "ClientId": "...",
          "ClientSecret": "...",
          "TenantId": "..."
        }
      },
      "ConstellationHost": "constellation.microcosm.blue",
      "CosmosDBAccountEndpoint": "https://example.documents.azure.com:443/",
      "CosmosDBAccountKey": "...",
      "DeviantArtClientId": "...",
      "DeviantArtClientSecret": "...",
      "DeviantArtUsername":  "...",
      "RedditAppId": "...",
      "RedditAppSecret": "...",
      "RedditUsername":  "...",
      "ActivityPubUsername": "userhere",
      "StorageAccountHostname": "example.blob.core.windows.net",
      "KeyVaultHostname": "example.vault.azure.net",
      "WeasylProxyHost": "www.example.com"
    }

The key vault is for a single encryption key called `activitypub` that is used to sign ActivityPub requests.

## Azure Deployment Guide

1. Create Azure resources:
    1. Create a Web app in the Azure portal, using the .NET 10 runtime stack.
        * Use the Identity pane to create a system-assigned managed identity for the web app.
    1. Create a Function App in the Azure portal, using the .NET 10 runtime stack. (If you are using a paid App Service plan for the web app, you could reuse that plan; otherwise, a Flex Consumption plan is fine.)
        * Use the Identity pane to create a system-assigned managed identity for the Function App.
    1. Create a Key Vault in the Azure portal; use Azure role-based access control (RBAC).
        * Create a 2048-bit RSA key in this vault called "activitypub".
        * Use access control (IAM) to assign yourself the "Key Vault Crypto Officer" role, and assign the "Key Vault Crypto User" role to the web and function apps' managed identities.
    1. Create an Azure Cosmos DB for NoSQL database. (You may want to enable key-based authentication if you find that Entra ID authentication slows down Pandacap's data access.)
        * If you are not using key-based authentication for Cosmos DB, follow the guide at https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-connect-role-based-access-control?pivots=azure-cli to assign control plane access to yourself and data plane access to the web and function apps' managed identities.
        * Use the Data Explorer pane in the Cosmos DB account to create a database called Pandacap, with a container named PandacapDbContext and a partition key named `/__partitionKey`.
    1. Find "Microsoft Entra ID" in the portal to view your directory, and create an enterprise application ("create your own application" --> "register an application to integrate with Microsoft Entra ID").
        * Set up a "web" redirect URI, such as `https://www.example.com/signin-oidc` (replacing www.example.com with the hostname of your web app).
        * In the Properties pane, **set "Assignment required" to "Yes"** and then assign yourself via the "Users and groups" pane.
    1. Create a storage account (for Blob Storage) with an appropriate access tier for your use case (hot, cool, or cold).
        * Use access control (IAM) to assign the "Storage Blob Data Contributor" role to the web app's managed identity.
        * Create a container called "blobs".
    1. Assign the following environment variables to the web app:
        * `Authentication:Microsoft:ClientId` (if you want to log in with Microsoft): The application (client) ID of the app registration you've created in Entra.
        * `Authentication:Microsoft:ClientSecret` (if you want to log in with Microsoft): The value of a client secret - you can create one from the "certificates & secrets" pane of the app registration (keep in mind that these have expiration dates).
        * `Authentication:Microsoft:TenantId` (if you want to log in with Microsoft): The directory (tenant) ID.
        * `DeviantArtClientId` (if you want to log in with DeviantArt): A client ID from https://www.deviantart.com/studio/apps.
        * `DeviantArtClientSecret` (if you want to log in with DeviantArt): A client secret from https://www.deviantart.com/studio/apps.
        * `DeviantArtUsername` (if you want to log in with DeviantArt): Only this user will be permitted to log in via DeviantArt; other valid DeviantArt users will be rejected.
        * `RedditAppId` (if you want to log in with Reddit): An app ID from https://developers.reddit.com/my/apps.
        * `RedditAppSecret` (if you want to log in with Reddit): An app secret from https://developers.reddit.com/my/apps.
        * `RedditUsername` (if you want to log in with Reddit): Only this user will be permitted to log in via Reddit; other valid Reddit users will be rejected.
    1. Assign the following environment variables to both the web app and the function app:
        * `ActivityPubUsername`: the username part of the ActivityPub handle (before the `@www.example.com` portion).
        * `ApplicationHostname`: the web app's hostname (e.g. `www.example.com`).
        * `ConstellationHost`: the hostname of a [Constellation](https://constellation.microcosm.blue) instance. Used for looking up interactions with your posts that are bridged to Bluesky via Bridgy Fed.
        * `CosmosDBAccountEndpoint`: the URL of the Cosmos DB server created above (e.g. https://www.example.net:443/).
        * `CosmosDBAccountKey` (optional): if you are using key-based authentication, the key goes here.
        * `KeyVaultHostname`: The hostname to the key vault created above.
        * `StorageAccountHostname`: The hostname to the storage account created above.
        * `WeasylProxyHost`: The hostname of a domain which contains the paths `/pandacap/weasyl_proxy.php` and `/pandacap/weasyl_submit.php`.
1. Clone the Pandacap git repository: <br/> `git clone https://github.com/IsaacSchemm/Pandacap`, with storage account key access disabled.
1. Open Pandacap.sln with Visual Studio.
1. Right-click on the Pandacap project and click Publish.
    * Create a new profile to deploy to your newly created Azure App Service.
    * Publish to the app service.
1. Right-click on the Pandacap.Functions project and click Publish.
    * Create a new profile to deploy to your newly created Function App.
    * Publish to the function app.
