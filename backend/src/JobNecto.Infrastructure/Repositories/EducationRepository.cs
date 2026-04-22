using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Repositories;

public class EducationRepository : EditableRepository<Education>
{
    public EducationRepository(AppDbContext context) : base(context)
    {
    }
}