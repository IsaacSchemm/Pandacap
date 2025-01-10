﻿namespace Pandacap.ConfigurationObjects

module UserAgentInformation =
    /// The application name (e.g. "Pandacap").
    let ApplicationName = "Pandacap"

    /// The Pandacap version number.
    let VersionNumber = "7.0.0-beta1"

    /// A URL to a website with more information about the application.
    let WebsiteUrl = "https://github.com/IsaacSchemm/Pandacap"

    /// The user agent string for outgoing ActivityPub requests.
    let UserAgent = $"{ApplicationName}/{VersionNumber} ({WebsiteUrl})"
