using Enarro.Domain.Common;
using Enarro.Domain.Documents;
using Enarro.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enarro.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable(TableName.Documents);

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasConversion(
                id => id.Value,
                value => DocumentId.From(value))
            .HasColumnName("Id");

        builder.Property(d => d.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(100);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(2000);

        // IAuditable
        builder.Property(d => d.CreatedOn);
        builder.Property(d => d.CreatedBy).HasMaxLength(256);
        builder.Property(d => d.LastModifiedOn);
        builder.Property(d => d.LastModifiedBy).HasMaxLength(256);

        // ISoftDeletable
        builder.Property(d => d.IsDeleted).HasDefaultValue(false);
        builder.Property(d => d.DeletedOn);
        builder.Property(d => d.DeletedBy).HasMaxLength(256);

        // Global query filter for soft delete
        builder.HasQueryFilter(d => !d.IsDeleted);

        // Owned collection: DocumentTag as value objects
        builder.OwnsMany(d => d.Tags, tag =>
        {
            tag.ToTable("DocumentTags");
            tag.WithOwner().HasForeignKey("DocumentId");

            tag.Property(t => t.TagKey)
                .HasColumnName("TagKey")
                .HasMaxLength(100)
                .IsRequired();

            tag.Property(t => t.TagValue)
                .HasColumnName("TagValue")
                .HasMaxLength(500)
                .IsRequired();
        });

        builder.Navigation(d => d.Tags)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => d.Status);
    }
}
