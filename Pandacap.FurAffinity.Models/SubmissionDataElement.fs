namespace Pandacap.FurAffinity.Models

open System

type SubmissionDataElement = {
    avatar_mtime: string
    description: string
    lower: string
    title: string
    username: string
} with
    member this.AvatarUrl = $"https://a.furaffinity.net/{Uri.EscapeDataString(this.avatar_mtime)}/{Uri.EscapeDataString(this.lower)}.gif"
