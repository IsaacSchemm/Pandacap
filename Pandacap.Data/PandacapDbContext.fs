namespace Pandacap.Data

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.EntityFrameworkCore

type PandacapDbContext(options: DbContextOptions<PandacapDbContext>) =
    inherit IdentityDbContext(options)
    
    member this.DeviantArtCredentials: DbSet<DeviantArtCredentials> = this.Set()
    member this.ATProtoCredentials: DbSet<ATProtoCredentials> = this.Set()
    member this.WeasylCredentials: DbSet<WeasylCredentials> = this.Set()
    member this.Avatars: DbSet<Avatar> = this.Set()
    member this.Posts: DbSet<Post> = this.Set()
    member this.AddressedPosts: DbSet<AddressedPost> = this.Set()
    member this.PostActivities: DbSet<PostActivity> = this.Set()
    member this.DeviantArtTextPostCheckStatuses: DbSet<DeviantArtTextPostCheckStatus> = this.Set()
    member this.InboxArtworkDeviations: DbSet<InboxArtworkDeviation> = this.Set()
    member this.InboxTextDeviations: DbSet<InboxTextDeviation> = this.Set()
    member this.InboxATProtoPosts: DbSet<InboxATProtoPost> = this.Set()
    member this.InboxActivityStreamsPosts: DbSet<InboxActivityStreamsPost> = this.Set()
    member this.InboxWeasylSubmissions: DbSet<InboxWeasylSubmission> = this.Set()
    member this.RssFeeds: DbSet<RssFeed> = this.Set()
    member this.RssFeedItems: DbSet<RssFeedItem> = this.Set()
    member this.Follows: DbSet<Follow> = this.Set()
    member this.Followers: DbSet<Follower> = this.Set()
    member this.CommunityBookmarks: DbSet<CommunityBookmark> = this.Set()
    member this.RemoteActivityPubFavorites: DbSet<RemoteActivityPubFavorite> = this.Set()
    member this.RemoteActivityPubReplies: DbSet<RemoteActivityPubReply> = this.Set()
    member this.ActivityPubOutboundActivities: DbSet<ActivityPubOutboundActivity> = this.Set()

    [<Obsolete("Only used for imported DeviantArt posts, a feature which is no longer available in Pandacap 4.0+")>]
    member this.UserPosts: DbSet<UserPost> = this.Set()

    member this.MigrateAsync(cancellationToken) = task {
        let! newPosts = this.Posts.ToListAsync(cancellationToken)
        this.Posts.RemoveRange(newPosts)

        do! this.SaveChangesAsync(cancellationToken) :> Task

        let! allPosts = this.UserPosts.ToListAsync(cancellationToken)
        for up in allPosts |> Seq.sortBy (fun p -> p.PublishedTime) do
            let post = new Post()
            post.BlueskyDID <- up.BlueskyDID
            post.BlueskyRecordKey <- up.BlueskyRecordKey
            post.Body <- $"<p>{up.Description}</p>"
            post.DeviantArtId <- up.Id
            post.DeviantArtUrl <- up.Url
            post.Id <- up.Id
            post.Images <- ResizeArray [
                if not (isNull up.Image) then
                    let i = new PostImage()
                    i.AltText <- up.AltText
                    i.Blob <- new PostBlobRef(Id = up.Image.Id, ContentType = up.Image.ContentType)
                    i.Thumbnails <- ResizeArray [
                        if not (isNull up.Thumbnail) then
                            new PostBlobRef(Id = up.Thumbnail.Id, ContentType = up.Thumbnail.ContentType)
                    ]
                    i
            ]
            post.PublishedTime <- up.PublishedTime
            post.Sensitive <- up.IsMature
            post.Summary <- if up.IsMature then "Mature content (unspecified)" else null
            post.Tags <- up.Tags
            post.Title <- if up.HideTitle then null else up.Title
            post.Type <-
                if up.Artwork then PostType.Artwork
                else if up.IsArticle then PostType.JournalEntry
                else PostType.StatusUpdate
            post.WeasylJournalId <- up.WeasylJournalId
            post.WeasylSubmitId <- up.WeasylSubmitId
            this.Posts.Add(post) |> ignore

        do! this.SaveChangesAsync(cancellationToken) :> Task
    }

    override _.OnModelCreating(builder) =
        base.OnModelCreating(builder)
        ignore [
            builder.Entity<IdentityRole>().Property(fun b -> b.ConcurrencyStamp).IsETagConcurrency()
            builder.Entity<IdentityUser>().Property(fun b -> b.ConcurrencyStamp).IsETagConcurrency()
        ]
