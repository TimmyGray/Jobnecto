using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VacancyConfiguration : IEntityTypeConfiguration<Vacancy>
{
    public void Configure(EntityTypeBuilder<Vacancy> builder)
    {
        builder.ToTable(
            "Vacancies",
            t =>
            {
                t.HasCheckConstraint("CK_Vacancies_SalaryMin", "\"SalaryMin\" >= 0");
                t.HasCheckConstraint("CK_Vacancies_SalaryMax", "\"SalaryMax\" >= 0");
                t.HasCheckConstraint(
                    "CK_Vacancies_MatchScore",
                    "\"MatchScore\" >= 0 AND \"MatchScore\" <= 1"
                );
            }
        );

        builder.HasKey(v => v.Id);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(v => v.Title).HasMaxLength(500);

        builder.Property(v => v.Description).HasMaxLength(5000);

        builder.Property(v => v.Company).HasMaxLength(100);

        builder.Property(v => v.CompanyWebsite).HasMaxLength(500);

        builder.Property(v => v.ExperienceLevel).HasMaxLength(100);

        builder.Property(v => v.IsChosen);

        builder.Property(v => v.IsHidden);

        builder.Property(v => v.Location).HasConversion<string>();

        builder.Property(v => v.WorkLocationType).HasConversion<string>();

        builder.Property(v => v.WorkTimeType).HasConversion<string>();

        builder
            .Property(v => v.JobCategories)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
            )
            .HasColumnType("jsonb");

        builder
            .Property(v => v.Skills)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
            )
            .HasColumnType("jsonb");

        builder.Property(v => v.SalaryMin).HasColumnType("decimal(18,2)");

        builder.Property(v => v.SalaryMax).HasColumnType("decimal(18,2)");

        builder.Property(v => v.MatchScore).HasColumnType("double precision");

        builder.Property(v => v.Currency).HasConversion<string>();

        builder
            .Property(v => v.JobSource)
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<JobSource>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Property(v => v.CreatedAt).HasDefaultValueSql("Now()").ValueGeneratedOnAdd();

        builder
            .Property(v => v.UpdatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(v => v.IsDeleted).HasDefaultValue(false);

        builder.Property(v => v.DeletedAt);
    }
}
