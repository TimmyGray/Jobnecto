using JobNecto.Application.Educations.Mappers;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.Educations;

/// <summary>
/// Handles education record update requests.
/// </summary>
public class UpdateEducationCommandHandler : IRequestHandler<UpdateEducationCommand, EducationResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEducationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public UpdateEducationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<EducationResult> Handle(UpdateEducationCommand request, CancellationToken cancellationToken)
    {
        var education = await _unitOfWork.EducationRepository.GetByIdAsync(request.EducationId, cancellationToken);

        if (education.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to update this education record.");

        education.ApplyUpdates(request);
        education.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.EducationRepository.UpdateAsync(education, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return education.ToEducationResult();
    }
}
