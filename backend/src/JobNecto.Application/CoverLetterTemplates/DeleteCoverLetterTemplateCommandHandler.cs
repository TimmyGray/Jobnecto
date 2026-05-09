using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Handles cover letter template soft-delete requests.
/// </summary>
public class DeleteCoverLetterTemplateCommandHandler : IRequestHandler<DeleteCoverLetterTemplateCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCoverLetterTemplateCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public DeleteCoverLetterTemplateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(DeleteCoverLetterTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.CoverLetterTemplateRepository.GetByIdAsync(
            request.CoverLetterTemplateId, cancellationToken);

        if (template.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to delete this cover letter template.");

        await _unitOfWork.CoverLetterTemplateRepository.SoftDeleteAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
