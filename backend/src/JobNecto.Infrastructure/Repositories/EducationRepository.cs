using JobNecto.Infrastructure.Persistance;

namespace JobNecto.Infrastructure.Repositories;

public class EducationRepository : EditableRepository<Education>
{
    public EducationRepository(AppDbContext context) : base(context)
    {
    }
}