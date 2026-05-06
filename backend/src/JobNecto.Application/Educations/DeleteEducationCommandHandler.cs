using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.Educations;

/// <summary>
/// Handles education record soft-delete requests.
/// </summary>
public class DeleteEducationCommandHandler : IRequestHandler<DeleteEducationCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEducationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public DeleteEducationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(DeleteEducationCommand request, CancellationToken cancellationToken)
    {
        var education = await _unitOfWork.EducationRepository.GetByIdAsync(request.EducationId, cancellationToken);

        if (education.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to delete this education record.");

        await _unitOfWork.EducationRepository.SoftDeleteAsync(education, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
