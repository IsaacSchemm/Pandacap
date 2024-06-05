using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Pandacap.Data
{
    public class PandacapDbContext(DbContextOptions<PandacapDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<ActivityPubOutboundActivity> ActivityPubOutboundActivities { get; set; }

        public DbSet<DeviantArtCredentials> DeviantArtCredentials { get; set; }

        public DbSet<DeviantArtArtworkDeviation> DeviantArtArtworkDeviations { get; set; }

        public DbSet<DeviantArtTextDeviation> DeviantArtTextDeviations { get; set; }

        public DbSet<Follower> Followers { get; set; }

        public DbSet<Follow> Follows { get; set; }

        public DbSet<InboxImageDeviation> InboxImageDeviations { get; set; }

        public DbSet<InboxTextDeviation> InboxTextDeviations { get; set; }

        public DbSet<RemoteActivityPubPost> RemoteActivityPubPosts { get; set; }

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
