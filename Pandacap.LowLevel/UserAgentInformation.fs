﻿namespace Pandacap.LowLevel

module UserAgentInformation =
    /// The application name (e.g. "Pandacap").
    let ApplicationName = "Pandacap"

    /// The Pandacap version number.
    let VersionNumber = "6.3.4"

    /// A URL to a website with more information about the application.
    let WebsiteUrl = "https://github.com/IsaacSchemm/Pandacap"

    /// The user agent string for outgoing ActivityPub requests.
    let UserAgent = $"{ApplicationName}/{VersionNumber} ({WebsiteUrl})"
