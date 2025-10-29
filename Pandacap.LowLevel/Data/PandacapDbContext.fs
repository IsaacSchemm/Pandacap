namespace Pandacap.Data

open Microsoft.EntityFrameworkCore

type PandacapDbContext(options: DbContextOptions<PandacapDbContext>) =
    inherit DbContext(options)

    member this.DeviantArtCredentials: DbSet<DeviantArtCredentials> = this.Set()
    member this.FurAffinityCredentials: DbSet<FurAffinityCredentials> = this.Set()
    member this.RedditCredentials: DbSet<RedditCredentials> = this.Set()
    member this.WeasylCredentials: DbSet<WeasylCredentials> = this.Set()
    member this.Avatars: DbSet<Avatar> = this.Set()
    member this.Posts: DbSet<Post> = this.Set()
    member this.Uploads: DbSet<Upload> = this.Set()
    member this.AddressedPosts: DbSet<AddressedPost> = this.Set()
    member this.PostActivities: DbSet<PostActivity> = this.Set()
    member this.DeviantArtTextPostCheckStatuses: DbSet<DeviantArtTextPostCheckStatus> = this.Set()
    member this.InboxArtworkDeviations: DbSet<InboxArtworkDeviation> = this.Set()
    member this.InboxTextDeviations: DbSet<InboxTextDeviation> = this.Set()
    member this.InboxActivityStreamsPosts: DbSet<InboxActivityStreamsPost> = this.Set()
    member this.InboxFurAffinityJournals: DbSet<InboxFurAffinityJournal> = this.Set()
    member this.InboxFurAffinitySubmissions: DbSet<InboxFurAffinitySubmission> = this.Set()
    member this.InboxWeasylJournals: DbSet<InboxWeasylJournal> = this.Set()
    member this.InboxWeasylSubmissions: DbSet<InboxWeasylSubmission> = this.Set()
    member this.GeneralFeeds: DbSet<GeneralFeed> = this.Set()
    member this.GeneralInboxItems: DbSet<GeneralInboxItem> = this.Set()
    member this.GeneralFavorites: DbSet<GeneralFavorite> = this.Set()
    member this.Follows: DbSet<Follow> = this.Set()
    member this.Followers: DbSet<Follower> = this.Set()
    member this.CommunityBookmarks: DbSet<CommunityBookmark> = this.Set()
    member this.ATProtoFeeds: DbSet<ATProtoFeed> = this.Set()
    member this.BlueskyLikeFeedItems: DbSet<BlueskyLikeFeedItem> = this.Set()
    member this.BlueskyPostFeedItems: DbSet<BlueskyPostFeedItem> = this.Set()
    member this.BlueskyRepostFeedItems: DbSet<BlueskyRepostFeedItem> = this.Set()
    member this.WhiteWindBlogEntryFeedItems: DbSet<WhiteWindBlogEntryFeedItem> = this.Set()
    member this.LeafletDocumentFeedItems: DbSet<LeafletDocumentFeedItem> = this.Set()
    member this.RemoteActivityPubAddressedPosts: DbSet<RemoteActivityPubAddressedPost> = this.Set()
    member this.RemoteActivityPubReplies: DbSet<RemoteActivityPubReply> = this.Set()
    member this.ActivityPubOutboundActivities: DbSet<ActivityPubOutboundActivity> = this.Set()
    member this.ActivityPubFavorites: DbSet<ActivityPubFavorite> = this.Set()
    member this.BlueskyPostFavorites: DbSet<BlueskyPostFavorite> = this.Set()
    member this.DeviantArtFavorites: DbSet<DeviantArtFavorite> = this.Set()
    member this.FurAffinityFavorites: DbSet<FurAffinityFavorite> = this.Set()
    member this.RedditUpvotedPosts: DbSet<RedditUpvotedPost> = this.Set()
    member this.WeasylFavoriteSubmissions: DbSet<WeasylFavoriteSubmission> = this.Set()

    override _.OnModelCreating(builder) =
        base.OnModelCreating(builder)
        ignore [
            builder.HasEmbeddedDiscriminatorName("Discriminator")
            builder.HasDiscriminatorInJsonIds()
            builder.HasShadowIds()
        ]
