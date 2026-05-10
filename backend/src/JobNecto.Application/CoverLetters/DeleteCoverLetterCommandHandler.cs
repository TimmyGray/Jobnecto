using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Handles cover letter soft-delete requests.
/// </summary>
public class DeleteCoverLetterCommandHandler : IRequestHandler<DeleteCoverLetterCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCoverLetterCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public DeleteCoverLetterCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(DeleteCoverLetterCommand request, CancellationToken cancellationToken)
    {
        var coverLetter = await _unitOfWork.CoverLetterRepository.GetByIdAsync(request.CoverLetterId, cancellationToken);

        if (coverLetter.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to delete this cover letter.");

        await _unitOfWork.CoverLetterRepository.SoftDeleteAsync(coverLetter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}