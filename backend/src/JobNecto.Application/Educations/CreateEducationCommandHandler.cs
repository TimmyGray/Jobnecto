using JobNecto.Application.Educations.Mappers;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.Educations;

/// <summary>
/// Handles creation of education records for the current user.
/// </summary>
public class CreateEducationCommandHandler : IRequestHandler<CreateEducationCommand, EducationResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEducationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public CreateEducationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<EducationResult> Handle(CreateEducationCommand request, CancellationToken cancellationToken)
    {
        var education = request.ToEntity();

        await _unitOfWork.EducationRepository.CreateAsync(education, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return education.ToEducationResult();
    }
}