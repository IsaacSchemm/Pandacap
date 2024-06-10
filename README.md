# Pandacap

"Posts (Articles and Notes) from DeviantArt Copied to ActivityPub"

An Azure-hosted, **single-user** bridge and inbox reader for DeviantArt.

This is **not**:
* a general-purpose bridge between DeviantArt and ActivityPub (only the instance owner's posts are broadcast)
* a general-purpose art website (only the instance owner can log in, and there are no upload or discoverability features)
* a general-purpose social media platform like Mastodon (image posts, text posts, replies/mentions, and shares are in separate feeds)

Things it does:

* Allow the owner to log in with their DeviantArt account and:
    * View image posts from users they follow on DeviantArt or ActivityPub
    * View text posts from users they follow on DeviantArt or ActivityPub
    * View ActivityPub mentions and replies
    * View shared (boosted) ActivityPub image posts
    * Mark ActivityPub posts as favorites
    * See which other ActivityPub users have liked or boosted a post
    * Follow and unfollow ActivityPub users
* Allow visitors to:
    * See the owner's DeviantArt submissions
    * See the owner's DeviantArt journals and status updates
    * See the owner's ActivityPub follows, followers, and favorites
* Make the owner's DeviantArt submissions, journals, and status updates available to ActivityPub servers such as Pixelfed and Mastodon

Things which will probably be added:

* Allow the owner to follow RSS/Atom feeds

Things which will probably not be added:

* Allow the owner to view shared (boosted) ActivityPub text posts
* Allow the owner to reply to an ActivityPub post, or mention an ActivityPub user in a post
* Expose ActivityPub likes and boosts to other users
* Expose ActivityFed "comments" (replies) to other users

Things it does not do, **by design**:

* Allow the owner to create ActivityPub posts that do not map to a DeviantArt post
* Mirror any actual image data from DeviantArt in its own database
* Expose any other user's DeviantArt posts or activity over ActivityPub
* Allow any user other than the instance owner to log in
* Render text posts and artwork posts within the same feed
* Render boosts and normal posts within the same feed

## Deployment

This application runs on the following Azure resources:

* A Cosmos DB NoSQL database
* An Azure Functions app
* A web app
* A Key Vault

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
to Cosmos DB using Entra authentication, which can lead ot slower performance.
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
