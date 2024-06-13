# Pandacap

An Azure-hosted, **single-user** bridge and inbox reader for DeviantArt.

This is **not**:
* a full bridge between DeviantArt and ActivityPub (only the instance owner's posts are broadcast)
* a general-purpose art website (only the instance owner can log in, and there are no upload or discoverability features)
* a microblogging platform (different post types are in separate feeds; shares are ignored by default and can be filtered based on post type)

Things it does:

* Allow the owner to log in with their DeviantArt account and:
    * Follow and unfollow ActivityPub users
    * Follow and unfollow Atom and RSS feeds
    * View image posts from DeviantArt accounts, ActivityPub actors, or Atom/RSS feeds they follow
    * View text posts from DeviantArt accounts, ActivityPub actors, or Atom/RSS feeds they follow
    * View ActivityPub mentions and replies
    * Mark ActivityPub posts as favorites
    * See which other ActivityPub users have liked or boosted the owner's posts
* Allow visitors to:
    * See the owner's DeviantArt submissions
    * See the owner's DeviantArt journals and status updates
    * See the owner's ActivityPub handle
    * See the owner's AT Protocol handle, if Bridgy Fed is connected
    * See the owner's ActivityPub follows, followers, and favorites
* Make the owner's DeviantArt submissions, journals, and status updates available to ActivityPub servers such as Pixelfed and Mastodon

Things which will probably not be added:

* Allow the owner to reply to an ActivityPub post, or mention an ActivityPub user in a post
* Expose ActivityPub likes and boosts to other users
* Expose ActivityFed "comments" (replies) to other users

Things it does not do, **by design**:

* Allow the owner to create new posts
* Mirror any actual image data from DeviantArt in its own database
* Expose any other user's DeviantArt posts or activity over ActivityPub
* Allow any user other than the instance owner to log in
* Render text posts and image posts within the same feed
* Render boosts and normal posts within the same feed

## Deployment

This application runs on the following Azure resources:

* A Cosmos DB NoSQL database
* An Azure Functions app
* A web app
* A Key Vault

Function app responsibilities:

* Every minute:
    * Try sending any unsent ActivityPub activities (any inbox that fails won't be retried for another hour)
* Every hour:
    * Find new DeviantArt posts by other users
    * Find and refresh DeviantArt posts made by the instance owner in the past 7 days (unless they are less than an hour old)
* Every day:
    * Remove any unsent ActivityPub activities that are more than a week old
* Every day:
    * Remove dismissed DeviantArt inbox notifications from the Pandacap database
        * The five most recent submissions of each type are kept, as a way to track Pandacap's position in the DeviantArt API feed
* Every month:
    * Find and refresh all DeviantArt posts by the instance owner (unless they are less than an hour old)
    * Refresh the instance owner's avatar

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
