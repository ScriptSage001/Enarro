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
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    
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
        
        // Configure UserEntity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();
                
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();
                
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsRequired();
                
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("User");
            
            // Unique index on email
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("idx_users_email");
                
            entity.HasIndex(e => e.Role)
                .HasDatabaseName("idx_users_role");
        });
        
        // Configure RefreshTokenEntity
        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Token)
                .HasMaxLength(500)
                .IsRequired();
            
            // Relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_refresh_tokens_user_id");
                
            entity.HasIndex(e => e.Token)
                .HasDatabaseName("idx_refresh_tokens_token");
                
            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("idx_refresh_tokens_expires_at");
        });
        
        // Update DocumentEntity relationship to User
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_documents_user_id");
        });
    }
}
