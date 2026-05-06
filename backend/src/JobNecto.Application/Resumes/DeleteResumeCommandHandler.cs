using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Handles resume soft-delete requests.
/// </summary>
public class DeleteResumeCommandHandler : IRequestHandler<DeleteResumeCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteResumeCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public DeleteResumeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
    {
        var resume = await _unitOfWork.ResumeRepository.GetByIdAsync(request.ResumeId, cancellationToken);

        if (resume.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to delete this resume.");

        await _unitOfWork.ResumeRepository.SoftDeleteAsync(resume, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
