# Pandacap

An Azure-hosted, ActivityPub-compatible art gallery and feed reader.

This is **not**:
* a bridge between DeviantArt and ActivityPub (only the instance owner's posts are exposed, and import is fully manual)
* a fully self-contained application (DeviantArt is the only implemented method for authentication and importing posts)
* a proper social media site (only the instance owner can log in, and there are no discoverability features)

Pandacap is a single-user application.
To log in, the instance owner must use DeviantArt account that matches the Pandacap configuration.

Once logged in, the instance owner can:

* Manage posts:
    * Import posts (artwork, journals, and status updates) from their DeviantArt account
    * Refresh posts by checking DeviantArt for updates
    * Set artwork alt text
    * Remove imported posts
* Manage follows:
    * Follow and unfollow ActivityPub users
    * Follow and unfollow Atom and RSS feeds
    * Attach an external Bluesky account
* View image posts from DeviantArt accounts, ActivityPub actors, Bluesky accounts, or Atom/RSS feeds they follow
* View text posts from DeviantArt accounts, ActivityPub actors, Bluesky accounts, or Atom/RSS feeds they follow
* View ActivityPub mentions and replies
* View ActivityPub and Bluesky boosts
* Mark ActivityPub posts as favorites
* See which other ActivityPub users have liked or boosted the owner's posts

They cannot:

* Create or edit posts without going through DeviantArt
* Reply to posts or @mention other users
* Sync their DeviantArt posts to the attached Bluesky account (Bridgy Fed can be used for outbound Bluesky support), although this may be added in the future

Visitors can:

* See the owner's DeviantArt submissions
* See the owner's DeviantArt journals and status updates
* See the owner's ActivityPub handle
* See the owner's AT Protocol handle, if Bridgy Fed is connected
* See the owner's ActivityPub follows, followers, and favorites

## Deployment

This application runs on the following Azure resources:

* A Cosmos DB NoSQL database
* An Azure Functions app
* A web app
* A Key Vault
* A storage account

The web app and function app must have the appropriate IAM permissions to access the storage account (Storage Blob Data Contributor) and the key vault (Key Vault Crypto User).

Function app responsibilities:

* `InboxCleanup` (every day at 9:00): clear dismissed Bluesky and DeviantArt inbox entries more than 7 days old
* `InboxIngest` (every hour at :10): check Bluesky and DeviantArt timelines for new posts within the last 3 days, and check RSS/Atom feeds for new posts
* `OutboxCleanup` (every day at 8:00): remove unsent outbound ActivityPub messages that have been pending for more than 7 days
* `SendOutbound` (every ten minutes): attempt to send any pending outbound ActivityPub messages (if a failure occurs, the recipient will be skipped for the next hour)

Application settings (for both the function app and the web app):

| Name                    | Purpose                        
| ----------------------- | -------------------------------------
| ApplicationHostname     | Public hostname of the app
| CosmosDBAccountEndpoint | URL of the database
| CosmosDBAccountKey      | Database key
| DeviantArtClientId      | OAuth client ID from DeviantArt
| DeviantArtClientSecret  | OAuth secret from DeviantArt
| DeviantArtUsername      | Instance owner's DeviantArt username
| KeyVaultHostname        | Key vault hostname

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
        "DeviantArtUsername": "example",
        "KeyVaultHostname": "example-kv.vault.azure.net"
      }
    }

Web app `local.settings.json` example:

    {
      "ApplicationHostname": "example.azurewebsites.net",
      "CosmosDBAccountEndpoint": "https://example-cosmos.documents.azure.com:443/",
      "CosmosDBAccountKey": "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000==",
      "DeviantArtClientId": "12345",
      "DeviantArtClientSecret": "00000000000000000000000000000000",
      "DeviantArtUsername": "example",
      "KeyVaultHostname": "example-kv.vault.azure.net"
    }

The key vault is for a single encryption key called `activitypub` that is used
to sign ActivityPub requests.
