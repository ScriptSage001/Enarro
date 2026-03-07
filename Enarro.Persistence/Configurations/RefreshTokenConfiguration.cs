using Enarro.Domain.Common;
using Enarro.Domain.Users;
using Enarro.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enarro.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(TableName.RefreshTokens);

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .HasMaxLength(512)
            .IsRequired();

        // Convert RefreshToken.UserId (UserId) to Guid for storage
        builder.Property(rt => rt.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .HasColumnName("UserId");

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500);

        // ITimeStamped
        builder.Property(rt => rt.CreatedOn);
        builder.Property(rt => rt.LastModifiedOn);

        builder.HasIndex(rt => rt.Token);
    }
}
