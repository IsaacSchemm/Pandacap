namespace Pandacap.Data

open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.EntityFrameworkCore

type PandacapDbContext(options: DbContextOptions<PandacapDbContext>) =
    inherit IdentityDbContext(options)
    
    member this.DeviantArtCredentials: DbSet<DeviantArtCredentials> = this.Set()
    member this.ATProtoCredentials: DbSet<ATProtoCredentials> = this.Set()
    member this.WeasylCredentials: DbSet<WeasylCredentials> = this.Set()
    member this.Avatars: DbSet<Avatar> = this.Set()
    member this.ProfileProperties: DbSet<ProfileProperty> = this.Set()
    member this.UserPosts: DbSet<UserPost> = this.Set()
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
    member this.RemoteActivityPubFavorites: DbSet<RemoteActivityPubFavorite> = this.Set()
    member this.ActivityPubInboundActivities: DbSet<ActivityPubInboundActivity> = this.Set()
    member this.ActivityPubOutboundActivities: DbSet<ActivityPubOutboundActivity> = this.Set()

    override _.OnModelCreating(builder) =
        base.OnModelCreating(builder)
        ignore [
            builder.Entity<IdentityRole>().Property(fun b -> b.ConcurrencyStamp).IsETagConcurrency()
            builder.Entity<IdentityUser>().Property(fun b -> b.ConcurrencyStamp).IsETagConcurrency()
        ]
