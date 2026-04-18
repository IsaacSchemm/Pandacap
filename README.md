# Pandacap

Demo: https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/

A single-user hobby project that combines a public art gallery (+ blog) and a private feed reader, with crossposting and ActivityPub S2S support.

## Design Philosophy

1. Pandacap is a gallery/blog application, not a social media platform, so a Pandacap instance should be branded with the admin's username, not the name of the application.
2. Pandacap should not show any content to logged-out users that was not either created or put there by the admin.
3. Pandacap's feed reader should keep shares separate from original content, and keep image content separate from text.
4. Pandacap should use an inbox paradigm for incoming content: posts should be added to the inbox when they arrive, and manually removed by the admin after they've read them.
5. No page on Pandacap should have infinite scroll by default; pages should have a maximum length and present a "next page" button when appropriate.
6. Pandacap should be deployable to Microsoft Azure in such a way as to minimize idle costs (at the expense of both performance and scalability, if necessary).

## Screenshots

* [Main Page](https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/UserPosts/e3a78ccc-02b4-4bfe-9c88-f0cbde2be5b2)
* [Inbox (Image Posts)](https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/UserPosts/0e21e890-cb4f-4353-aa19-bed8b59fd9e7)
* [Inbox (Text Posts)](https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/UserPosts/2b3ee683-505f-40a8-b738-d82629c405ea)
* [Inbox (Shares)](https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/UserPosts/1bf13e41-4f30-4f5d-9c6f-36688c180505)
* [View Remote Post](https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/UserPosts/47f2c898-5d08-4ec3-a241-55b7f7c707be)
* [View Bluesky Post](https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/UserPosts/f4d45fb8-35b0-41de-8122-8bf5cbe96115)
* [Notifications](https://pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net/UserPosts/8d9b18b2-2520-4bfe-84c3-3b6f009d5a4d)

## Home Page

The home page shows:

* Your avatar and username
* Your ActivityPub handle
* Your atproto handle, if any
* Links to your attached accounts
* Links to users and feeds you follow
* Up to 8 of your **image posts** (from the past three months)
* Up to 12 image posts from your **favorites** (from the past week)
* Up to 5 of your **text posts** (from the past month)
* Up to 5 of your **links** (from the past month)

## Features

* Create **image posts**, **status updates**, **journal entries**, and **links**, which are available on the site, via RSS/Atom, and via ActivityPub
* Crosspost your image posts and text posts to attached DeviantArt, Fur Affinity, or Weasyl accounts
* Follow other users and feeds via RSS/Atom, ActivityPub, or atproto
* View posts from users or feeds you follow in the **inbox**
    * The inbox is split into **image posts**, **text posts**, **shares**, and **podcasts**
    * Up to 100 posts are shown on one page, and posts within the same page are grouped by author
    * Checkboxes are used to remove posts you've read from the inbox
    * Non-ActivityPub posts are periodically imported in the background
* View **notifications** from activity on your posts or from your attached accounts
* Add posts to your **favorites**
    * ActivityPub and Bluesky posts can be added manually
    * Favorites from DeviantArt, Fur Affinity, and Weasyl are imported automatically

## Techincal Information

Pandacap is written on ASP.NET Core with a mix of C# and F#.
It is designed to run on Microsoft Azure, using high-level resources like Azure App Service and Cosmos DB.
This version is not designed to run on a VPS or a local machine.

To log in, the instance owner must use one of the following:
* A Microsoft account that they have explicitly allowed in the associated Entra ID app registration.
  This means authorization is the reponsibility of your Entra ID registration, so only one user account should be allowed access.
* A DeviantArt account whose username matches the app setting `DeviantArtUsername`.

### ActivityPub

Pandacap acts as an ActivityPub S2S server, hosting a single actor.

Most public posts are sent to followers via ActivityPub.
Pandacap also has the concept of an "addressed post", an unlisted message with specific recipients; these are used for ActivityPub replies and for top-level posts to Lemmy communities.

|                | ID/URL format         | Federated as
| -------------- | --------------------- | ------------
| Artwork        | `/UserPosts/...`      | Note
| Journal entry  | `/UserPosts/...`      | Article
| Status update  | `/UserPosts/...`      | Note
| Link           | `/UserPosts/...`      | Note
| Scraps         | `/UserPosts/...`      | 
| Addressed post | `/AddressedPosts/...` | Note

Pandacap allows you to follow ActivityPub users.
Posts from users you follow are sent to the appropriate section of the Pandacap inbox,
and you can choose to ignore boosts or to treat all posts from the user as text posts when they arrive in your inbox.

Pandacap stores some information about an ActivityPub post (like the ID, author, and thumbnail) when adding it to the inbox,
but when you click on an ActivityPub post as a logged-in user, Pandacap will always fetch the post from its original instance.

Activities (such as `Like`, `Dislike`, `Announce`) and replies to your posts are shown in the Notifications section.

Adding an ActivityPub post to your Favorites will send a `Like` activity.

### atproto

#### Following

Pandacap allows you to follow atproto accounts as feeds. Individual DIDs or handles can be provided to Pandacap, which will store the DID and then treat the account as a feed. Each time it refreshes the feed, it will resolve the DID to a PDS and then query that PDS directly to detect changes, and (if necessary) for profile updates and any new posts (up to 20 per feed per run).

For each user, you can choose whether to follow Bluesky posts, reposts, and/or likes. Other lexicons, like standard.site, are not supported yet.

Pandacap will also look for Bluesky profile data (name and icon) when it refreshes the feed (every 8 hours, just like for RSS feeds).

If you view a Bluesky post while logged in, and Pandacap detects that the post is available via Bridgy Fed, it will show the bridged ActivityPub version of the post, which allows you to like it (by adding it to your favorites) or reply to it.

All data is fetched (unauthenticated) from the individual user's PDS, using `com.atproto.repo.listRecords`; the Bluesky AppView is not used.

Pandacap's only use of Bluesky infrastructure is its CDN, for showing thumbnails of posts on the public Favorites page.

#### Posting

Pandacap allows you to enable and disable Bridgy Fed from the profile page while logged in.

Periodically, if you've posted a new post (and have Bridgy Fed enabled), Pandacap will find the bridged version of your post and add a "View on Bluesky" link to the post's page.
(This is then also used to populate your atproto handle and links on the home page.)

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

Weasyl support relies on a PHP proxy script (included in this repository).

### RSS / Atom

Pandacap can follow RSS and Atom feeds. New posts are added to the appropriate section of the Pandacap inbox.

Posts with `audio/mpeg` attachments are sent to the Podcasts section, where you can download the file in its original form or as a set of 5-minute-long uncompressed .wma files (for burning to CD).

Pandacap also makes your own posts available over RSS and Atom; the Gallery and Text Posts pages have links to these feeds.

RSS and Atom posts cannot be added to Favorites. This is primarily because individual posts (podcast episodes, for example) may not have public web page URLs, which Pandacap needs so it has a link to send visitors to.

## Deployment

This application runs on:

* A Cosmos DB NoSQL database (for data storage)
* An Azure Functions app
* A web app
* A Key Vault
* A blob storage account

ASP.NET Core Identity is backed by an in-memory database, so after the application is rebooted, it will prompt you again to create a user account after you authenticate. Pandacap's actual data is stored in Cosmos DB, and Pandacap's own code only ever checks whether you're validly logged in or not.

The web app and function app must have the appropriate IAM permissions to access the storage account (Storage Blob Data Contributor) and the key vault (Key Vault Crypto User).

Function app responsibilities include:

* adding the Bluesky DID and record keys (for the "View on Bluesky" link) to posts of yours that have been bridged
* checking accounts for new favorites / likes / upvotes
* deleting any dismissed inbox entries more than 7 days old
* automatically dismissing active inbox entries (the 200 most recent posts, and any other posts newer than 30 days, will be kept)
* checking feeds and connected platforms for new posts
* removing unsent outbound ActivityPub messages that have been pending for more than 7 days
* attempting to send any pending outbound ActivityPub messages (if a failure occurs, the recipient will be skipped for the next hour)

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
| CosmosDBAccountEndpoint | URL of the database
| CosmosDBAccountKey      | Database key
| DeviantArtClientId      | OAuth client ID from DeviantArt
| DeviantArtClientSecret  | OAuth secret from DeviantArt
| KeyVaultHostname        | Key vault hostname
| StorageAccountHostname  | Hostname of the Azure blob storage account used for storing images associated with your posts or avatar
| WeasylProxyHost         | Hostname that has `/pandacap/weasyl_proxy.php` and `/pandacap/weasyl_submit.php`

Application settings (for the web app only):

| Name                                  | Purpose
| ------------------------------------- | -----------------------------------------------------------------------
| Authentication:Microsoft:TenantId     | Tenant ID of your Entra (AAD) directory
| Authentication:Microsoft:ClientId     | Application (client) ID of the app registration you've created in Entra
| Authentication:Microsoft:ClientSecret | A client secret generated for the app registration
| VectorSearchEmbeddingsEndpoint        | (optional) The target URI for an embedding model in Microsoft Foundry
| VectorSearchSearchEndpoint            | (optional) The URI of an Azure AI Search resource
| VectorSearchIndexName                 | (optional) The name of the index in Azure AI Search to populate and use

[Vector search](https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-create-index)
uses embeddings generated from the title, alt text, description, links, and tags in your posts.
Vector search is not enabled in the Pandacap demo instance.

The key vault is for a single encryption key called `activitypub` that is used to sign ActivityPub requests.

## Azure Deployment Guide

1. Create Azure resources:
    1. Create a Web app in the Azure portal, using the .NET 10 runtime stack.
        * Use the Identity pane to create a system-assigned managed identity for the web app.
    1. Create a Function App in the Azure portal, using the .NET 10 runtime stack. (If you are using a paid App Service plan for the web app, you could reuse that plan; otherwise, a Consumption plan is fine.
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
    1. Assign the following environment variables to both the web app and the function app:
        * `ActivityPubUsername`: the username part of the ActivityPub handle (before the `@www.example.com` portion).
        * `ApplicationHostname`: the web app's hostname (e.g. `www.example.com`).
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
