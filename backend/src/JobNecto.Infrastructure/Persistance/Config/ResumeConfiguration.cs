using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.ToTable("Resumes", r =>
        {
            r.HasCheckConstraint("CK_Resumes_Salary", "Salary >= 0");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .HasMaxLength(500);

        builder.Property(r => r.Salary)
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.Currency)
            .HasConversion<string>();

        builder.Property(r => r.Skills)
            .HasColumnType("jsonb");

        builder.Property(r => r.WorkLocationType)
            .HasConversion<string>();

        builder.Property(r => r.Experience)
            .HasConversion<string>();

        builder.Property(r => r.Projects)
            .HasColumnType("jsonb");

        builder.Property(r => r.Certifications)
            .HasColumnType("jsonb");

        builder.Property(r => r.Languages)
            .HasColumnType("jsonb");

        builder.Property(r => r.Locations)
            .HasColumnType("jsonb");

        builder.Property(r => r.ExcludedWords)
            .HasColumnType("jsonb");

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.UpdatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasOne<Resume>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Educations)
            .WithMany(e => e.Resumes)
            .UsingEntity(j => j.ToTable("ResumeEducations"));
    }
}
