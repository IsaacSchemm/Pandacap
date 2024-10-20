﻿namespace Pandacap.LowLevel

type ApplicationInformation = {
    /// The host / domain name used by Pandacap.
    /// May or may not be the same as the domain in the ActivityPub actor's handle.
    ApplicationHostname: string

    /// The username of the Pandacap actor (used in the @ handle).
    Username: string

    /// The host / domain name used in the ActivityPub actor's preferred handle.
    /// May or may not be the same as Pandacap's domain.
    HandleHostname: string

    /// The host / domain name of the key vault used for the signing key for
    /// ActivityPub.
    KeyVaultHostname: string
} with
    /// The application name (e.g. "Pandacap").
    member _.ApplicationName = "Pandacap"

    /// The Pandacap version number.
    member _.VersionNumber = "4.0.1"

    /// A URL to a website with more information about the application.
    member _.WebsiteUrl = "https://github.com/IsaacSchemm/Pandacap"

    /// The user agent string for outgoing ActivityPub requests.
    member this.UserAgent = $"{this.ApplicationName}/{this.VersionNumber} ({this.WebsiteUrl})"
