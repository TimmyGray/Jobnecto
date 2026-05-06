using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Repositories;

public class EducationRepository : SoftDeletableRepository<Education>
{
    public EducationRepository(AppDbContext context) : base(context)
    {
    }
}