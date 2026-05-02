namespace Pandacap.Notifications.Composite

open System
open FSharp.Control
open Pandacap.Extensions
open Pandacap.UI.Badges
open Pandacap.Notifications.Composite.Interfaces
open Pandacap.Notifications.Interfaces

type internal CompositeNotificationHandler(
    notificationHandlers: INotificationHandler seq
) =
    let wrapInErrorHandler (underlyingHandler: INotificationHandler) = {
        new INotificationHandler with
            member _.GetNotificationsAsync() = asyncSeq {
                let enumerable = underlyingHandler.GetNotificationsAsync()
                let enumerator = enumerable.GetAsyncEnumerator()

                let mutable more = true
                while more do
                    more <- false

                    try
                        let! hasItem = enumerator.MoveNextAsync().AsTask() |> Async.AwaitTask
                        if hasItem then
                            yield enumerator.Current
                            more <- true
                    with ex ->
                        yield {
                            new INotification with
                                member _.ActivityName = ex.GetType().Name
                                member _.Badge = Badges.Pandacap
                                member _.Url = null
                                member _.UserName = $"{underlyingHandler.GetType().Name}: {ex.Message}"
                                member _.UserUrl = null
                                member _.PostUrl = null
                                member _.Timestamp = DateTimeOffset.UtcNow
                        }
        }
    }

    interface ICompositeNotificationHandler with
        member _.GetNotificationsAsync() =
            let sequences =
                notificationHandlers
                |> Seq.map wrapInErrorHandler
                |> Seq.map (fun handler -> handler.GetNotificationsAsync())

            sequences.MergeNewest(fun notification -> notification.Timestamp)
