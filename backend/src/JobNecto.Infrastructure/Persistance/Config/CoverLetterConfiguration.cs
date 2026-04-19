using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobNecto.Infrastructure.Persistance.Config;

public class CoverLetterConfiguration : IEntityTypeConfiguration<CoverLetter>
{
    public void Configure(EntityTypeBuilder<CoverLetter> builder)
    {
        builder.ToTable("CoverLetters");

        builder.HasKey(cl => cl.Id);

        builder.Property(cl => cl.Content).IsRequired().HasColumnType("text");

        builder.Property(cl => cl.CreatedAt).HasDefaultValueSql("Now()").ValueGeneratedOnAdd();

        builder
            .Property(cl => cl.UpdatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(cl => cl.IsDeleted).HasDefaultValue(false);

        builder.Property(cl => cl.DeletedAt);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(cl => cl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<Vacancy>()
            .WithMany()
            .HasForeignKey(cl => cl.VacancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
