using Enarro.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enarro.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500);

        // ITimeStamped
        builder.Property(rt => rt.CreatedOn);
        builder.Property(rt => rt.LastModifiedOn);

        builder.HasIndex(rt => rt.Token);
    }
}
