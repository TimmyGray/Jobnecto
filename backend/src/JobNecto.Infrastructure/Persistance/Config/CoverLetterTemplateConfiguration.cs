using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace JobNecto.Infrastructure.Persistance.Config;

public class CoverLetterTemplateConfiguration : IEntityTypeConfiguration<CoverLetterTemplate>
{
    public void Configure(EntityTypeBuilder<CoverLetterTemplate> builder)
    {
        builder.ToTable("CoverLetterTemplates");

        builder.HasKey(clt => clt.Id);

        builder.Property(clt => clt.Name).IsRequired();

        builder.Property(clt => clt.Content).IsRequired();

        builder.Property(clt => clt.CreatedAt).HasDefaultValueSql("Now()").ValueGeneratedOnAdd();

        builder
            .Property(clt => clt.UpdatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(clt => clt.IsDeleted).HasDefaultValue(false);

        builder.Property(clt => clt.DeletedAt);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(clt => clt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}