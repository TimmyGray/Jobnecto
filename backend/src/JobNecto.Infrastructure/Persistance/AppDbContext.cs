using Microsoft.EntityFrameworkCore;

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
    }
}
