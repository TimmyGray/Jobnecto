using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Repositories;

public class CoverLetterRepository : EditableRepository<CoverLetter>
{
    public CoverLetterRepository(AppDbContext context) : base(context)
    {
    }
}