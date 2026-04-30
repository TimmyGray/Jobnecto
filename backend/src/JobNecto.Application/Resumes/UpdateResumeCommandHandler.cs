using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes.Mappers;
using MediatR;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Handles resume update requests.
/// </summary>
public class UpdateResumeCommandHandler : IRequestHandler<UpdateResumeCommand, ResumeResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateResumeCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public UpdateResumeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ResumeResult> Handle(UpdateResumeCommand request, CancellationToken cancellationToken)
    {
        var resume = await _unitOfWork.ResumeRepository.GetByIdAsync(request.ResumeId, cancellationToken);

        if (resume.UserId != request.UserId)
            throw new ForbiddenException("You do not have permission to update this resume.");

        resume.ApplyUpdates(request);
        resume.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ResumeRepository.UpdateAsync(resume, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return resume.ToResumeResult();
    }
}
