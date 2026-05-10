using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Handles retrieval of a single cover letter by ID for the authenticated user.
/// </summary>
public class GetCoverLetterQueryHandler : IRequestHandler<GetCoverLetterQuery, CoverLetterDetailResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCoverLetterQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public GetCoverLetterQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CoverLetterDetailResult> Handle(GetCoverLetterQuery request, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.CoverLetterRepository.GetDetailByIdAsync(request.CoverLetterId, cancellationToken);

        if (result is null || result.UserId != request.UserId)
            throw new NotFoundException("CoverLetter", request.CoverLetterId);

        return result;
    }
}