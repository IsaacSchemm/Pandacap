namespace Pandacap.FurAffinity.Models

open System

type Note = {
    note_id: int
    subject: string
    userDisplayName: string
    time: DateTimeOffset
}
