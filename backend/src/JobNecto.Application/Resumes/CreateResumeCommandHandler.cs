using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes.Mappers;
using MediatR;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Handler for business logic of creating a new Resume.
/// Uses IUnitOfWork to persist the entity.
/// </summary>
public class CreateResumeCommandHandler : IRequestHandler<CreateResumeCommand, ResumeResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateResumeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Implements resume creation: maps request to entity, persists via repository.
    /// </summary>
    public async Task<ResumeResult> Handle(CreateResumeCommand request, CancellationToken cancellationToken)
    {
        // 1. Map command to Domain entity
        // UserId is already set by the controller
        var resume = request.ToEntity();

        // 2. Persist using the repository from UnitOfWork
        // CreateAsync is part of IRepository<T> which IEditableRepository<T> inherits
        await _unitOfWork.ResumeRepository.CreateAsync(resume, cancellationToken);
        
        // 3. Save changes in a single transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Return result through mapper
        return resume.ToResumeResult();
    }
}
