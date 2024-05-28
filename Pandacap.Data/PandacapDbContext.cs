using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Pandacap.Data
{
    public class PandacapDbContext(DbContextOptions<PandacapDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<DeviantArtCredentials> DeviantArtCredentials { get; set; }

        public DbSet<DeviantArtInboxArtworkPost> DeviantArtInboxArtworkPosts { get; set; }

        public DbSet<DeviantArtInboxTextPost> DeviantArtInboxTextPosts { get; set; }

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
