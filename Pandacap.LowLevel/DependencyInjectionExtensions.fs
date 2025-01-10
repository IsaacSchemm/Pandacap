namespace Pandacap.LowLevel

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ConfigurationObjects

[<System.Obsolete>]
type HostInformationProvider(appInfo: ApplicationInformation) = 
    interface Pandacap.ActivityPub.IHostInformationProvider with
        member _.ApplicationHostname = appInfo.ApplicationHostname
