namespace Pandacap.FurAffinity.Models

open System

type Notification = {
    time: DateTimeOffset
    text: string
    journalId: Nullable<int>
}
