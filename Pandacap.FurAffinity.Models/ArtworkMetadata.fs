namespace Pandacap.FurAffinity.Models

type ArtworkMetadata = {
    title: string
    message: string
    keywords: string list
    cat: int
    scrap: bool
    atype: int
    species: int
    gender: int
    rating: Rating
    lock_comments: bool
    folder_ids: Set<int64>
}
