namespace Pandacap.Weasyl.Scraping

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.Weasyl.Scraping.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddWeasylScraper(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IWeasylScraper, WeasylScraper>()
