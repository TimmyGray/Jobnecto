using JobNecto.Application.CoverLetterTemplates.Mappers;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Handles cover letter template update requests.
/// </summary>
public class UpdateCoverLetterTemplateCommandHandler : IRequestHandler<UpdateCoverLetterTemplateCommand, CoverLetterTemplateResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCoverLetterTemplateCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public UpdateCoverLetterTemplateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CoverLetterTemplateResult> Handle(UpdateCoverLetterTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.CoverLetterTemplateRepository.GetByIdAsync(request.CoverLetterTemplateId, cancellationToken);

        if (template.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to update this cover letter template.");

        template.ApplyUpdates(request);
        template.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CoverLetterTemplateRepository.UpdateAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template.ToCoverLetterTemplateResult();
    }
}