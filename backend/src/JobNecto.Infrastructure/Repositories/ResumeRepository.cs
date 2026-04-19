using JobNecto.Infrastructure.Persistance;
namespace JobNecto.Infrastructure.Repositories;

public class ResumeRepository : EditableRepository<Resume>
{
    public ResumeRepository(AppDbContext context) : base(context)
    {
    }
}