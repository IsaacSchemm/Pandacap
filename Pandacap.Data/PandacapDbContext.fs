namespace Pandacap.Data

open System
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.EntityFrameworkCore

type PandacapDbContext(options: DbContextOptions<PandacapDbContext>) =
    inherit IdentityDbContext(options)

    member this.ATProtoCredentials: DbSet<ATProtoCredentials> = this.Set()
    member this.DeviantArtCredentials: DbSet<DeviantArtCredentials> = this.Set()
    member this.FurAffinityCredentials: DbSet<FurAffinityCredentials> = this.Set()
    member this.WeasylCredentials: DbSet<WeasylCredentials> = this.Set()
    member this.SheezyArtAccounts: DbSet<SheezyArtAccount> = this.Set()
    member this.Avatars: DbSet<Avatar> = this.Set()
    member this.BridgyFedBridges: DbSet<BridgyFedBridge> = this.Set()
    member this.Posts: DbSet<Post> = this.Set()
    member this.Uploads: DbSet<Upload> = this.Set()
    member this.AddressedPosts: DbSet<AddressedPost> = this.Set()
    member this.PostActivities: DbSet<PostActivity> = this.Set()
    member this.DeviantArtTextPostCheckStatuses: DbSet<DeviantArtTextPostCheckStatus> = this.Set()
    member this.InboxArtworkDeviations: DbSet<InboxArtworkDeviation> = this.Set()
    member this.InboxTextDeviations: DbSet<InboxTextDeviation> = this.Set()
    member this.InboxBlueskyPosts: DbSet<InboxBlueskyPost> = this.Set()
    member this.InboxActivityStreamsPosts: DbSet<InboxActivityStreamsPost> = this.Set()
    member this.InboxFurAffinityJournals: DbSet<InboxFurAffinityJournal> = this.Set()
    member this.InboxFurAffinitySubmissions: DbSet<InboxFurAffinitySubmission> = this.Set()
    member this.InboxWeasylJournals: DbSet<InboxWeasylJournal> = this.Set()
    member this.InboxWeasylSubmissions: DbSet<InboxWeasylSubmission> = this.Set()
    member this.RssFeeds: DbSet<RssFeed> = this.Set()
    member this.RssFeedItems: DbSet<RssFeedItem> = this.Set()
    member this.Follows: DbSet<Follow> = this.Set()
    member this.Followers: DbSet<Follower> = this.Set()
    member this.CommunityBookmarks: DbSet<CommunityBookmark> = this.Set()
    member this.BlueskyFollows: DbSet<BlueskyFollow> = this.Set()
    member this.RemoteActivityPubReplies: DbSet<RemoteActivityPubReply> = this.Set()
    member this.ActivityPubOutboundActivities: DbSet<ActivityPubOutboundActivity> = this.Set()
    member this.ActivityPubLikes: DbSet<ActivityPubLike> = this.Set()
    member this.ActivityPubAnnounces: DbSet<ActivityPubAnnounce> = this.Set()
    member this.BlueskyLikes: DbSet<BlueskyLike> = this.Set()
    member this.BlueskyReposts: DbSet<BlueskyRepost> = this.Set()
    member this.DeviantArtFavorites: DbSet<DeviantArtFavorite> = this.Set()
    member this.FurAffinityFavorites: DbSet<FurAffinityFavorite> = this.Set()
    member this.SheezyArtFavorites: DbSet<SheezyArtFavorite> = this.Set()
    member this.WeasylFavoriteSubmissions: DbSet<WeasylFavoriteSubmission> = this.Set()

    override _.OnModelCreating(builder) =
        base.OnModelCreating(builder)
        ignore [
            builder.Entity<IdentityRole>().Property(fun b -> b.ConcurrencyStamp).IsETagConcurrency()
            builder.Entity<IdentityUser>().Property(fun b -> b.ConcurrencyStamp).IsETagConcurrency()
        ]
