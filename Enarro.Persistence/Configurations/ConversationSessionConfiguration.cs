using Enarro.Domain.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enarro.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ConversationSession"/>.
/// </summary>
internal class ConversationSessionConfiguration : IEntityTypeConfiguration<ConversationSession>
{
    public void Configure(EntityTypeBuilder<ConversationSession> builder)
    {
        builder.ToTable("ConversationSessions");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.SessionId).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.UpdatedAt);

        builder.Property(s => s.SessionId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Title).HasMaxLength(200);

        builder.HasMany(s => s.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .HasPrincipalKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
