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
* Crosspost your image posts and text posts to attached Bluesky, DeviantArt, Fur Affinity, or Weasyl accounts
* View posts from users or feeds you follow in the **inbox**, split among **image posts**, **text posts**, **shares**, and **podcasts**, and grouped by author
    * Non-ActivityPub posts are periodically imported (~3 times per day)
* View **notifications** from activity on your posts or from your attached accounts
* Add ActivityPub and Bluesky posts to your **favorites**
* Automatically import **favorites** from DeviantArt, Fur Affinity, and Weasyl

Some of the things Pandacap does **not** do:

* Act as an OAuth server.
* Host more than one user account.
* Host any public-facing content that is not intentionally placed there by the server admin.
* Create posts with more than one attached image.
* Attach images to replies.
* Automatically crosspost to Bluesky, DeviantArt, FA, or Weasyl. (This is done manually through the Pandacap user interface.)
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
You can also add "likes" of your own on others' ActivityPub posts, but you must first add the post to your Favorites.

### ATProto

Pandacap allows you to follow atproto accounts as feeds, without your own atproto account.
For each user, you can choose whether to follow Bluesky posts, Bluesky reposts, Bluesky likes, and/or WhiteWind blog entries.
Pandacap will also look for Bluesky profile data for each user when refreshing the feed.

You can add Bluesky posts to your Favorites.

When you click on a Bluesky post as a logged-in user, Pandacap will always fetch the post from its original PDS.

Pandacap also allows you to attach an atproto account. This can be used to crosspost to Bluesky, in roughly the same way crossposting to other platforms works in Pandacap.
It also adds Bluesky notifications to the Notifications section, and allows you to add "likes" on Bluesky posts or reply to them.

Pandacap talks directly to the PDS for most operations, and only uses the Bluesky AppView for populating the Notifications section.
(Bluesky's CDN is used for thumbnails and avatars.)

### DeviantArt / Fur Affinity / Weasyl

Pandacap interacts with these platforms through a combination of scraping and APIs, and requires valid account credentials.
You can crosspost to any or all of these platforms and view their notifications in the Notifications section.

Posts from users you follow on these platforms will appear in the Pandacap inbox, and posts you add to your favorites on these platforms will appear in Pandacap's Favorites automatically.

Fur Affinity support relies on [FAExport](https://faexport.spangle.org.uk/) for most functions, and Weasyl support relies on a PHP proxy script (included in this repository).

### Furry Network / Reddit / Sheezy.Art

Pandacap can monitor accounts on these platforms and automatically add new favorites (or in Reddit's case, upvotes of top-level posts) to its Favorites section.
No other integration with these platforms is currently available.

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
* **Pandacap.HighLevel**: Contains shared Pandacap code, including Bluesky and Weasyl abstractions, RSS/Atom feed support, and code to assemble the user's notifications and favorites into composite lists.

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

* `FavoriteIngest`
    * check accounts for new favorites / likes / upvotes
* `InboxCleanup`
    * clear dismissed inbox entries more than 7 days old
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
    * For the redirect URI, choose "Web" and type `https://localhost:7206/signin-microsoft`
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
| ComputerVisionEndpoint                | URL of the Azure AI Vision endpoint for generating sample alt text

The CosmosDBAccountKey is optional; without it, Pandacap will try to connect
to Cosmos DB using Entra authentication, which can lead to slower performance.
See [Crowmask](https://github.com/IsaacSchemm/Crowmask/) for an example of how
to set that up.

Function app `local.settings.json` example:

    {
      "IsEncrypted": false,
      "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "ApplicationHostname": "example.azurewebsites.net",
        "CosmosDBAccountEndpoint": "https://example-cosmos.documents.azure.com:443/",
        "CosmosDBAccountKey": "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000==",
        "DeviantArtClientId": "12345",
        "DeviantArtClientSecret": "00000000000000000000000000000000",
        "KeyVaultHostname": "example-kv.vault.azure.net"
      }
    }

Web app `local.settings.json` example:

    {
      "ApplicationHostname": "example.azurewebsites.net",
      "Authentication": {
        "Microsoft": {
          "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
          "ClientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
          "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
        }
      },
      "ComputerVisionEndpoint": "https://example-cv.cognitiveservices.azure.com/",
      "CosmosDBAccountEndpoint": "https://example-cosmos.documents.azure.com:443/",
      "CosmosDBAccountKey": "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000==",
      "DeviantArtClientId": "12345",
      "DeviantArtClientSecret": "00000000000000000000000000000000",
      "EntraClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
      "StorageAccountHostname": "example.blob.core.windows.net",
      "KeyVaultHostname": "example-kv.vault.azure.net"
    }

The key vault is for a single encryption key called `activitypub` that is used to sign ActivityPub requests.
