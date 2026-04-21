using Microsoft.EntityFrameworkCore;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Persistance;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Resume> Resumes { get; set; }
    public DbSet<Education> Educations { get; set; }
    public DbSet<CoverLetter> CoverLetters { get; set; }
    public DbSet<CoverLetterTemplate> CoverLetterTemplates { get; set; }
    public DbSet<Vacancy> Vacancies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Configure global query filters for soft-deletable entities
        // These filters automatically exclude soft-deleted records from all queries
        ConfigureSoftDeleteFilters(modelBuilder);
    }

    private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        // User: exclude soft-deleted users from all queries
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        // Resume: exclude soft-deleted resumes from all queries
        modelBuilder.Entity<Resume>()
            .HasQueryFilter(r => !r.IsDeleted);

        // Education: exclude soft-deleted educations from all queries
        modelBuilder.Entity<Education>()
            .HasQueryFilter(e => !e.IsDeleted);

        // CoverLetter: exclude soft-deleted cover letters from all queries
        modelBuilder.Entity<CoverLetter>()
            .HasQueryFilter(cl => !cl.IsDeleted);

        // CoverLetterTemplate: exclude soft-deleted templates from all queries
        modelBuilder.Entity<CoverLetterTemplate>()
            .HasQueryFilter(clt => !clt.IsDeleted);

        // Vacancy: exclude soft-deleted vacancies from all queries
        modelBuilder.Entity<Vacancy>()
            .HasQueryFilter(v => !v.IsDeleted);
    }
}
