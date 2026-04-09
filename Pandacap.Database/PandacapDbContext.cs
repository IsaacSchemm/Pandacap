using Microsoft.EntityFrameworkCore;

namespace Pandacap.Database
{
    public class PandacapDbContext(DbContextOptions<PandacapDbContext> options) : DbContext(options)
    {
        public DbSet<DeviantArtCredentials> DeviantArtCredentials { get; set; }
        public DbSet<FurAffinityCredentials> FurAffinityCredentials { get; set; }
        public DbSet<WeasylCredentials> WeasylCredentials { get; set; }
        public DbSet<Avatar> Avatars { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Upload> Uploads { get; set; }
        public DbSet<AddressedPost> AddressedPosts { get; set; }
        public DbSet<PostActivity> PostActivities { get; set; }
        public DbSet<DeviantArtTextPostCheckStatus> DeviantArtTextPostCheckStatuses { get; set; }
        public DbSet<InboxArtworkDeviation> InboxArtworkDeviations { get; set; }
        public DbSet<InboxTextDeviation> InboxTextDeviations { get; set; }
        public DbSet<InboxActivityStreamsPost> InboxActivityStreamsPosts { get; set; }
        public DbSet<InboxFurAffinityJournal> InboxFurAffinityJournals { get; set; }
        public DbSet<InboxFurAffinitySubmission> InboxFurAffinitySubmissions { get; set; }
        public DbSet<InboxWeasylJournal> InboxWeasylJournals { get; set; }
        public DbSet<InboxWeasylSubmission> InboxWeasylSubmissions { get; set; }
        public DbSet<GeneralFeed> GeneralFeeds { get; set; }
        public DbSet<GeneralInboxItem> GeneralInboxItems { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<CommunityBookmark> CommunityBookmarks { get; set; }
        public DbSet<ATProtoFeed> ATProtoFeeds { get; set; }
        public DbSet<BlueskyLikeFeedItem> BlueskyLikeFeedItems { get; set; }
        public DbSet<BlueskyPostFeedItem> BlueskyPostFeedItems { get; set; }
        public DbSet<BlueskyRepostFeedItem> BlueskyRepostFeedItems { get; set; }
        public DbSet<RemoteActivityPubAddressedPost> RemoteActivityPubAddressedPosts { get; set; }
        public DbSet<RemoteActivityPubReply> RemoteActivityPubReplies { get; set; }
        public DbSet<ActivityPubOutboundActivity> ActivityPubOutboundActivities { get; set; }
        public DbSet<ActivityPubFavorite> ActivityPubFavorites { get; set; }
        public DbSet<BlueskyPostFavorite> BlueskyPostFavorites { get; set; }
        public DbSet<DeviantArtFavorite> DeviantArtFavorites { get; set; }
        public DbSet<FurAffinityFavorite> FurAffinityFavorites { get; set; }
        public DbSet<WeasylFavoriteSubmission> WeasylFavoriteSubmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasEmbeddedDiscriminatorName("Discriminator");
            modelBuilder.HasDiscriminatorInJsonIds();
            modelBuilder.HasShadowIds();
        }
    }
}
