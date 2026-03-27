using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.ToTable("Educations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Specialization).HasMaxLength(100);

        builder.Property(e => e.Degree).HasConversion<string>().IsRequired();

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("Now()").ValueGeneratedOnAdd();

        builder
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAddOrUpdate();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
