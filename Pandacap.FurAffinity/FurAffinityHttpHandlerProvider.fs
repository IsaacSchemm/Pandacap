namespace Pandacap.FurAffinity

open System
open System.Net.Http

type internal FurAffinityHttpHandlerProvider() =
    let handler = lazy new SocketsHttpHandler(
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5L))

    member _.GetOrCreateHandler() = handler.Value
