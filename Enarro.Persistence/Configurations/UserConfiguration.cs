using Enarro.Domain.Common;
using Enarro.Domain.Users;
using Enarro.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enarro.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .HasColumnName("Id");

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();

            email.HasIndex(e => e.Value).IsUnique();
        });

        builder.OwnsOne(u => u.Name, name =>
        {
            name.Property(n => n.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(100)
                .IsRequired();

            name.Property(n => n.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasMaxLength(50)
            .HasDefaultValue("User");

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        // IAuditable
        builder.Property(u => u.CreatedOn);
        builder.Property(u => u.CreatedBy).HasMaxLength(256);
        builder.Property(u => u.LastModifiedOn);
        builder.Property(u => u.LastModifiedBy).HasMaxLength(256);

        // ISoftDeletable
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
        builder.Property(u => u.DeletedOn);
        builder.Property(u => u.DeletedBy).HasMaxLength(256);

        // Global query filter for soft delete
        builder.HasQueryFilter(u => !u.IsDeleted);

        // Relationship: User owns RefreshTokens
        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.RefreshTokens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
