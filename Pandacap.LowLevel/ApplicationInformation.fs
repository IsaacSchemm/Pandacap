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

    /// The URL to a PHP script that proxies requests to Weasyl (to avoid an
    /// IP address filter on Azure's outgoing IP address blocks).
    WeasylProxy: string
}
