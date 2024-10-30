# Pandacap

A single-user, Azure-hosted, ActivityPub-compatible art gallery and feed reader built on ASP.NET Core.

For more information, see Views/About/Index.cshtml.

Supported platforms and protocols:

|             | Crosspost  | Inbox | Reply | Notifications          | Authentication
| ----------- | ---------- | ----- | ----- | ---------------------- | ------------------------
| ActivityPub | ✓          | ✓     | ✓     | ✓ (Activites, Replies) |
| Bluesky     | ✓ (Manual) | ✓     |       | ✓                      | PDS / DID / Password
| DeviantArt  | ✓ (Manual) | ✓     |       | ✓ (Messages, Notes)    | OAuth (ASP.NET Identity)
| RSS / Atom  | ✓          | ✓     |       |                        |
| Weasyl      | ✓ (Manual) | ✓     |       | Partial                | API Key

Pandacap is a single-user application.
To log in, the instance owner must use a Microsoft account that they have explicitly allowed in the associated Entra ID app registration.

> **Any authenticated user can access the same data**.
> This means authorization is the sole reponsibility of your Entra ID registration, so only one user account should be allowed access.

A DeviantArt account cannot be used to set up the Pandacap account, but once attached to the existing account,
either it or the Microsoft account it can be used to log in.

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

| Name                                  | Purpose
| ------------------------------------- | ----------------------------------------------------
| ActivityPubUsername                   | Username to use for ActivityPub and on the home page
| ApplicationHostname                   | Public hostname of the app
| CosmosDBAccountEndpoint               | URL of the database
| CosmosDBAccountKey                    | Database key
| DeviantArtClientId                    | OAuth client ID from DeviantArt
| DeviantArtClientSecret                | OAuth secret from DeviantArt
| KeyVaultHostname                      | Key vault hostname

Application settings (for the web app only):

| Name                                  | Purpose
| ------------------------------------- | -----------------------------------------------------------------------
| Authentication:Microsoft:TenantId     | Tenant ID of your Entra (AAD) directory
| Authentication:Microsoft:ClientId     | Application (client) ID of the app registration you've created in Entra
| Authentication:Microsoft:ClientSecret | A client secret generated for the app registration

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
