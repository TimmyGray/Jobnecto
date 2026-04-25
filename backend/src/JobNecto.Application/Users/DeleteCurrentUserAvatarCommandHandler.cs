using JobNecto.Application.Interfaces;
using JobNecto.Application.Users.Mappers;
using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Handles current-user avatar deletion.
/// </summary>
public class DeleteCurrentUserAvatarCommandHandler : IRequestHandler<DeleteCurrentUserAvatarCommand, GetCurrentUserResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAvatarStorageService _avatarStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCurrentUserAvatarCommandHandler"/> class.
    /// </summary>
    public DeleteCurrentUserAvatarCommandHandler(IUnitOfWork unitOfWork, IAvatarStorageService avatarStorageService)
    {
        _unitOfWork = unitOfWork;
        _avatarStorageService = avatarStorageService;
    }

    /// <inheritdoc />
    public async Task<GetCurrentUserResult> Handle(DeleteCurrentUserAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            await _avatarStorageService.DeleteUserAvatarAsync(request.UserId, cancellationToken);
        }

        user.Avatar = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.UserRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToGetCurrentUserResult();
    }
}
