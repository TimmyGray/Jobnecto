using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;

namespace JobNecto.Infrastructure.Repositories;

public class CoverLetterTemplateRepository : SoftDeletableRepository<CoverLetterTemplate>
{
    public CoverLetterTemplateRepository(AppDbContext context) : base(context)
    {
    }
}