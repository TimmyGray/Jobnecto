using JobNecto.Application.CoverLetters.Mappers;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Handles creation of cover letters for the current user.
/// </summary>
public class CreateCoverLetterCommandHandler : IRequestHandler<CreateCoverLetterCommand, CreateCoverLetterResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCoverLetterCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public CreateCoverLetterCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CreateCoverLetterResult> Handle(
        CreateCoverLetterCommand request,
        CancellationToken cancellationToken)
    {
        var vacancy = await _unitOfWork.VacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken);

        // Return 404 for non-owned vacancies to avoid leaking resource existence.
        if (vacancy.UserId != request.UserId)
            throw new NotFoundException("Vacancy", request.VacancyId);

        var coverLetter = request.ToEntity();
        var now = DateTime.UtcNow;
        coverLetter.CreatedAt = now;
        coverLetter.UpdatedAt = now;

        try
        {
            await _unitOfWork.CoverLetterRepository.CreateAsync(coverLetter, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("A cover letter for this vacancy already exists.");
        }

        return coverLetter.ToCreateResult();
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            var message = current.Message;
            if (string.IsNullOrWhiteSpace(message))
                continue;

            if (message.Contains("23505", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}