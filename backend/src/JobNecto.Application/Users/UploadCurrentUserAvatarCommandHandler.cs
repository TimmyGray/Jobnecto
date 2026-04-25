using JobNecto.Application.Interfaces;
using JobNecto.Application.Users.Mappers;
using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Handles current-user avatar uploads.
/// </summary>
public class UploadCurrentUserAvatarCommandHandler : IRequestHandler<UploadCurrentUserAvatarCommand, GetCurrentUserResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAvatarStorageService _avatarStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadCurrentUserAvatarCommandHandler"/> class.
    /// </summary>
    public UploadCurrentUserAvatarCommandHandler(IUnitOfWork unitOfWork, IAvatarStorageService avatarStorageService)
    {
        _unitOfWork = unitOfWork;
        _avatarStorageService = avatarStorageService;
    }

    /// <inheritdoc />
    public async Task<GetCurrentUserResult> Handle(UploadCurrentUserAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);

        await using var avatarStream = new MemoryStream(request.Content, writable: false);
        var uploadResult = await _avatarStorageService.UploadUserAvatarAsync(
            request.UserId,
            avatarStream,
            request.FileName,
            request.ContentType,
            cancellationToken);

        user.Avatar = uploadResult.SecureUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.UserRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToGetCurrentUserResult();
    }
}
