using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users.Mappers;
using JobNecto.Domain.Entities;
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
        User user;
        try
        {
            user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException("User", request.UserId);
        }

        return user.ToGetCurrentUserResult();
    }
}
