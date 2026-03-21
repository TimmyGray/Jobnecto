using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
            {
                t.HasCheckConstraint("CK_Users_Age", "Age > 0 AND Age < 100");
                t.HasCheckConstraint("CK_Users_Email", "Email ~* '^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$'");
                t.HasCheckConstraint("CK_Users_Phone", "Phone ~* '^\\+[1-9]\\d{1,14}$'");
            }
        );

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .HasMaxLength(20);

        builder.Property(u => u.SecondName)
            .HasMaxLength(20);

        builder.Property(u => u.Age)
            .HasColumnType("int");

        builder.Property(u => u.Login)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Password)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(50);

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.Location)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.Skills)
            .HasColumnType("jsonb");

        builder.Property(u => u.Languages)
            .HasColumnType("jsonb");

        builder.Property(u => u.AboutMe)
            .HasMaxLength(5000);

        builder.Property(u => u.Certificates)
            .HasColumnType("jsonb");

        builder.Property(u => u.Projects)
            .HasColumnType("jsonb");

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("Now()")
            .ValueGeneratedOnAddOrUpdate();
    }
}
