using JobNecto.Infrastructure.Persistance;
namespace JobNecto.Infrastructure.Repositories;

public class CoverLetterRepository : EditableRepository<CoverLetter>
{
    public CoverLetterRepository(AppDbContext context) : base(context)
    {
    }
}