using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Repositories;

public class CoverLetterTemplateRepository : EditableRepository<CoverLetterTemplate>
{
    public CoverLetterTemplateRepository(AppDbContext context) : base(context)
    {
    }
}