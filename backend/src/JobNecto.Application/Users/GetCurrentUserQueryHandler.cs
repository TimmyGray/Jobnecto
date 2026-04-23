using JobNecto.Application.Interfaces;
using JobNecto.Application.Users.Mappers;
using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Handles current-user profile retrieval.
/// </summary>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentUserQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves the current authenticated user profile.
    /// </summary>
    public async Task<GetCurrentUserResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
        return user.ToGetCurrentUserResult();
    }
}
