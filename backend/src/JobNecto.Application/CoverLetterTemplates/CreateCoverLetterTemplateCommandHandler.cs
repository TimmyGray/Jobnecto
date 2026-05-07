using JobNecto.Application.CoverLetterTemplates.Mappers;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Handles creation of cover letter templates for the current user.
/// </summary>
public class CreateCoverLetterTemplateCommandHandler : IRequestHandler<CreateCoverLetterTemplateCommand, CoverLetterTemplateResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCoverLetterTemplateCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public CreateCoverLetterTemplateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CoverLetterTemplateResult> Handle(
        CreateCoverLetterTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = request.ToEntity();
        var now = DateTime.UtcNow;
        template.CreatedAt = now;
        template.UpdatedAt = now;

        await _unitOfWork.CoverLetterTemplateRepository.CreateAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template.ToCoverLetterTemplateResult();
    }
}
