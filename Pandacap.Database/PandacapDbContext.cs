using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Pandacap.Database
{
    public interface IOfflinePlatformDataCache
    {
        Task<T?> TryGetAsync<T>(
            OfflinePlatformDataCacheItem.CachedPlatformDataType type,
            CancellationToken cancellationToken = default) where T : class;

        Task UpdateAsync<T>(
            OfflinePlatformDataCacheItem.CachedPlatformDataType type,
            T value,
            CancellationToken cancellationToken = default) where T : class;
    }

    public class PandacapDbContext(DbContextOptions<PandacapDbContext> options) : DbContext(options), IOfflinePlatformDataCache
    {
        public DbSet<DeviantArtCredentials> DeviantArtCredentials { get; set; }
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
        public DbSet<StandardSiteDocumentFeedItem> StandardSiteDocumentFeedItems { get; set; }
        public DbSet<RemoteActivityPubAddressedPost> RemoteActivityPubAddressedPosts { get; set; }
        public DbSet<RemoteActivityPubReply> RemoteActivityPubReplies { get; set; }
        public DbSet<ActivityPubOutboundActivity> ActivityPubOutboundActivities { get; set; }
        public DbSet<ActivityPubFavorite> ActivityPubFavorites { get; set; }
        public DbSet<BlueskyPostFavorite> BlueskyPostFavorites { get; set; }
        public DbSet<DeviantArtFavorite> DeviantArtFavorites { get; set; }
        public DbSet<FurAffinityFavorite> FurAffinityFavorites { get; set; }
        public DbSet<WeasylFavoriteSubmission> WeasylFavoriteSubmissions { get; set; }
        public DbSet<CanonicalMedium> CanonicalMediums { get; set; }
        public DbSet<CanonicalSetting> CanonicalSettings { get; set; }
        public DbSet<CanonicalCharacter> CanonicalCharacters { get; set; }
        public DbSet<CanonicalSpecies> CanonicalSpecies { get; set; }
        public DbSet<OfflinePlatformDataCacheItem> CachedPlatformData { get; set; }
        public DbSet<FurAffinityNotification> FurAffinityNotifications { get; set; }
        public DbSet<FurAffinityNote> FurAffinityNotes { get; set; }
        public DbSet<WeasylNotification> WeasylNotifications { get; set; }
        public DbSet<WeasylNote> WeasylNotes { get; set; }

        public IOfflinePlatformDataCache OfflinePlatformDataCache => this;

        async Task<T?> IOfflinePlatformDataCache.TryGetAsync<T>(
            OfflinePlatformDataCacheItem.CachedPlatformDataType type,
            CancellationToken cancellationToken) where T : class
        {
            var platformData = await CachedPlatformData
                .Where(d => d.Type == type)
                .SingleOrDefaultAsync(cancellationToken);

            if (platformData == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<T?>(platformData.Json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        async Task IOfflinePlatformDataCache.UpdateAsync<T>(
            OfflinePlatformDataCacheItem.CachedPlatformDataType type,
            T value,
            CancellationToken cancellationToken) where T : class
        {
            var platformData = await CachedPlatformData
                .Where(d => d.Type == type)
                .SingleOrDefaultAsync(cancellationToken);

            if (platformData == null)
            {
                platformData = new() { Type = type };
                CachedPlatformData.Add(platformData);
            }

            platformData.Json = JsonSerializer.Serialize(value);

            await SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasEmbeddedDiscriminatorName("Discriminator");
            modelBuilder.HasDiscriminatorInJsonIds();
            modelBuilder.HasShadowIds();
        }
    }
}
