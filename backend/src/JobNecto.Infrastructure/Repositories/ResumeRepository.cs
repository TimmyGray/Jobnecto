using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Repositories;

public class ResumeRepository : EditableRepository<Resume>
{
    public ResumeRepository(AppDbContext context) : base(context)
    {
    }
}