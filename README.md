# Pandacap

"Posts (Articles and Notes) from DeviantArt Copied to ActivityPub"

An Azure-hosted, **single-user** ActivityPub bridge and inbox reader for DeviantArt.

Things it does:

* Allow the owner to log in with their DeviantArt account and:
    * View new DeviantArt submissions (from follows)
    * View new DeviantArt journals and status updates (from follows)
    * View new ActivityPub image posts (from follows + mentions and replies)
    * View new ActivityPub text posts (from follows + mentions and replies)
    * Mark ActivityPub posts as favorites
    * See which other ActivityPub users have liked or boosted a post
    * Follow and unfollow ActivityPub users
* Allow visitors to:
    * See the owner's DeviantArt submissions
    * See the owner's DeviantArt journals and status updates
    * See the owner's ActivityPub follows, followers, and favorites
* Make the owner's DeviantArt submissions, journals, and status updates available to ActivityPub servers such as Pixelfed and Mastodon

Things it does not do, but which could be added:

* Allow the owner to follow RSS/Atom feeds
* Allow the owner to create ActivityPub posts that do not map to a DeviantArt post
* Allow the owner to reply to an ActivityPub post, or mention an ActivityPub user in a post (you can reply from another ActivityPub account instead)
* Allow the owner to see posts which their ActivityPub follows have boosted
* Expose ActivityFed "comments" (replies) to other users
* Present a single public page that includes the owner's ActivityPub favorites alongside their DeviantArt favorites

Things it does not do, **by design**:

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

| Name                    | Purpose                         | Example
| ----------------------- | ------------------------------- | -------
| ApplicationHostname     | Public hostname of the app      | example.azurewebsites.net
| CosmosDBAccountEndpoint | URL of the database             | https://example-cosmos.documents.azure.com:443/
| CosmosDBAccountKey      | Database key                    | 00000000000000000000000000000000000000000000000000000000000000000000000000000000000000==
| DeviantArtClientId      | OAuth client ID from DeviantArt | 12345
| DeviantArtClientSecret  | OAuth secret from DeviantArt    | 00000000000000000000000000000000
| DeviantArtUsername      | Instance owner's DA username    | example
| KeyVaultHostname        | Key vault hostname              | example-kv.vault.azure.net

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

The key vault is for a single encryption key called `activitypub` that is used
to sign ActivityPub requests.
