using Ape.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ape.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<UserProfiles> UserProfiles { get; set; }

        // System Credentials Management DbSets
        public DbSet<SystemCredential> SystemCredentials { get; set; }
        public DbSet<CredentialAuditLog> CredentialAuditLogs { get; set; }

        // Email Logging
        public DbSet<EmailLog> EmailLogs { get; set; }

        // System Settings
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // Document Library
        public DbSet<PDFCategory> PDFCategories { get; set; }
        public DbSet<CategoryFile> CategoryFiles { get; set; }

        // Image Gallery
        public DbSet<GalleryCategory> GalleryCategories { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }

        // More Links
        public DbSet<LinkCategory> LinkCategories { get; set; }
        public DbSet<CategoryLink> CategoryLinks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PDFCategory configuration
            modelBuilder.Entity<PDFCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).ValueGeneratedOnAdd();

                // Self-referential FK for hierarchical folders
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(e => e.ChildCategories)
                    .HasForeignKey(e => e.ParentCategoryID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ParentCategoryID);
            });

            // GalleryCategory configuration
            modelBuilder.Entity<GalleryCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).ValueGeneratedOnAdd();

                // Self-referential FK for hierarchical categories
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(e => e.ChildCategories)
                    .HasForeignKey(e => e.ParentCategoryID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ParentCategoryID);
            });

            // GalleryImage configuration
            modelBuilder.Entity<GalleryImage>(entity =>
            {
                entity.HasKey(e => e.ImageID);
                entity.Property(e => e.ImageID).ValueGeneratedOnAdd();
            });
        }
    }
}

