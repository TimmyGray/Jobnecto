using JobNecto.Infrastructure.Persistance;

namespace JobNecto.Infrastructure.Repositories;

public class CoverLetterTemplateRepository : EditableRepository<CoverLetterTemplate>
{
    public CoverLetterTemplateRepository(AppDbContext context) : base(context)
    {
    }
}