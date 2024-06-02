﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Pandacap.Data
{
    public class PandacapDbContext(DbContextOptions<PandacapDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<ActivityPubInboxImagePost> ActivityPubInboxImagePosts { get; set; }

        public DbSet<ActivityPubInboxTextPost> ActivityPubInboxTextPosts { get; set; }

        public DbSet<ActivityPubOutboundActivity> ActivityPubOutboundActivities { get; set; }

        public DbSet<DeviantArtCredentials> DeviantArtCredentials { get; set; }

        public DbSet<DeviantArtInboxArtworkPost> DeviantArtInboxArtworkPosts { get; set; }

        public DbSet<DeviantArtInboxTextPost> DeviantArtInboxTextPosts { get; set; }

        public DbSet<DeviantArtArtworkDeviation> DeviantArtArtworkDeviations { get; set; }

        public DbSet<DeviantArtTextDeviation> DeviantArtTextDeviations { get; set; }

        public DbSet<Follower> Followers { get; set; }

        public DbSet<Following> Followings { get; set; }

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
