# Pandacap

An Azure-hosted, ActivityPub-compatible art gallery and feed reader.

This is **not**:
* a bridge between DeviantArt and ActivityPub (only the instance owner's posts are exposed, and import is fully manual)
* a fully self-contained application (DeviantArt is the only implemented method for authentication and importing posts)
* a proper social media site (only the instance owner can log in, and there are no discoverability features)

Pandacap is a single-user application.
To log in, the instance owner must use the DeviantArt account that matches the Pandacap configuration.

For more information, see Views/About/Index.cshtml.

## Deployment

This application runs on the following Azure resources:

* A Cosmos DB NoSQL database
* An Azure Functions app
* A web app
* A Key Vault
* A storage account

The web app and function app must have the appropriate IAM permissions to access the storage account (Storage Blob Data Contributor) and the key vault (Key Vault Crypto User).

Function app responsibilities:

* `InboxCleanup` (every day at 9:00)
    * clear dismissed inbox entries more than 7 days old
* `InboxIngest` (every hour at :10)
    * check Bluesky feed for new posts
    * check DeviantArt feed for new posts
    * check users followed on DeviantArt for new journals and status updates
    * check RSS/Atom feeds for new posts
* `OutboxCleanup` (every day at 8:00)
    * remove unsent outbound ActivityPub messages that have been pending for more than 7 days
* `SendOutbound` (every ten minutes)
    * attempt to send any pending outbound ActivityPub messages (if a failure occurs, the recipient will be skipped for the next hour)

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
