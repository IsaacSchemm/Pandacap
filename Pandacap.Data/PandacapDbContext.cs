using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Pandacap.Data
{
    public class PandacapDbContext(DbContextOptions<PandacapDbContext> options) : IdentityDbContext(options)
    {
        //public PandacapDbContext() : this(
        //    new DbContextOptionsBuilder<PandacapDbContext>()
        //    .UseInMemoryDatabase($"{Guid.NewGuid()}")
        //    .Options) { }

        public DbSet<ActivityPubInboundActivity> ActivityPubInboundActivities { get; set; }

        public DbSet<ActivityPubOutboundActivity> ActivityPubOutboundActivities { get; set; }

        public DbSet<Avatar> Avatars { get; set; }

        public DbSet<DeviantArtCredentials> DeviantArtCredentials { get; set; }

        public DbSet<Feed> Feeds { get; set; }

        public DbSet<FeedItem> FeedItems { get; set; }

        public DbSet<Follower> Followers { get; set; }

        public DbSet<Follow> Follows { get; set; }

        public DbSet<InboxArtworkDeviation> InboxArtworkDeviations { get; set; }

        public DbSet<InboxTextDeviation> InboxTextDeviations { get; set; }

        public DbSet<ProfileProperty> ProfileProperties { get; set; }

        public DbSet<InboxActivityPubAnnouncement> InboxActivityPubAnnouncements { get; set; }

        public DbSet<RemoteActivityPubFavorite> RemoteActivityPubFavorites { get; set; }

        public DbSet<InboxActivityPubPost> InboxActivityPubPosts { get; set; }

        public DbSet<UserPost> UserPosts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<IdentityRole>()
                .Property(b => b.ConcurrencyStamp)
                .IsETagConcurrency();
            builder.Entity<IdentityUser>()
                .Property(b => b.ConcurrencyStamp)
                .IsETagConcurrency();
        }
    }
}
