using JobNecto.Application.CoverLetters.Mappers;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Handles update requests for cover letter content.
/// </summary>
public class UpdateCoverLetterCommandHandler : IRequestHandler<UpdateCoverLetterCommand, CoverLetterUpdateResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCoverLetterCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public UpdateCoverLetterCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CoverLetterUpdateResult> Handle(UpdateCoverLetterCommand request, CancellationToken cancellationToken)
    {
        var coverLetter = await _unitOfWork.CoverLetterRepository.GetByIdAsync(request.CoverLetterId, cancellationToken);

        if (coverLetter.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to update this cover letter.");

        coverLetter.Content = request.Content;
        coverLetter.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CoverLetterRepository.UpdateAsync(coverLetter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coverLetter.ToUpdateResult();
    }
}