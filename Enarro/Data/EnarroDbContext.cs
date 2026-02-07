using Microsoft.EntityFrameworkCore;
using Enarro.Data.Entities;

namespace Enarro.Data;

/// <summary>
/// Database context for Enarro application
/// </summary>
public class EnarroDbContext : DbContext
{
    public EnarroDbContext(DbContextOptions<EnarroDbContext> options) : base(options) { }
    
    public DbSet<DocumentEntity> Documents { get; set; }
    public DbSet<DocumentTagEntity> DocumentTags { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure DocumentEntity
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsRequired();
                
            entity.Property(e => e.ContentType)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired();
                
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(100);
                
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);
            
            // Indexes
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_documents_status");
                
            entity.HasIndex(e => e.UploadedAt)
                .HasDatabaseName("idx_documents_uploaded_at");
        });
        
        // Configure DocumentTagEntity
        modelBuilder.Entity<DocumentTagEntity>(entity =>
        {
            entity.ToTable("document_tags");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TagKey)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.TagValue)
                .HasMaxLength(500)
                .IsRequired();
            
            // Relationship
            entity.HasOne(e => e.Document)
                .WithMany(d => d.Tags)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index
            entity.HasIndex(e => new { e.TagKey, e.TagValue })
                .HasDatabaseName("idx_document_tags_key_value");
        });
    }
}
