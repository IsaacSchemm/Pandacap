namespace Pandacap.FurAffinity

open System
open System.Net.Http
open Pandacap.FurAffinity.Interfaces

type FurAffinityHttpHandlerProvider() =
    let handler = lazy new SocketsHttpHandler(
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5L))

    interface IFurAffinityHttpHandlerProvider with
        member _.GetOrCreateHandler() = handler.Value
