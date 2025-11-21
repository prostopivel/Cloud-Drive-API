using Microsoft.EntityFrameworkCore;

namespace FileMetadata.Infrastructure.Data
{
    public class FileMetadataDbContext : DbContext
    {
        public FileMetadataDbContext(DbContextOptions<FileMetadataDbContext> options) : base(options)
        {
        }

        public DbSet<Core.Entities.FileMetadata> FileMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Core.Entities.FileMetadata>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.UploadedAt);
                entity.HasIndex(e => e.IsDeleted);

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.OriginalName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.StoragePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Size)
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.UploadedAt)
                    .IsRequired();

                // Query filter to exclude soft-deleted records by default
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
        }
    }
}
