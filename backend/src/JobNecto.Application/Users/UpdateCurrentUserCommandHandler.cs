using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users.Mappers;
using JobNecto.Domain.Enums;
using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Handles updates to current-user profile fields.
/// </summary>
public class UpdateCurrentUserCommandHandler : IRequestHandler<UpdateCurrentUserCommand, GetCurrentUserResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCurrentUserCommandHandler"/> class.
    /// </summary>
    public UpdateCurrentUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<GetCurrentUserResult> Handle(UpdateCurrentUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (request.LoginName != null)
        {
            var normalizedLogin = request.LoginName.Trim();
            if (!string.Equals(normalizedLogin, user.Login, StringComparison.Ordinal))
            {
                var existingByLogin = await _unitOfWork.UserRepository.GetByLoginAsync(normalizedLogin, cancellationToken);
                if (existingByLogin != null && existingByLogin.Id != user.Id)
                {
                    throw new ConflictException($"User with login '{normalizedLogin}' already exists.");
                }

                user.Login = normalizedLogin;
            }
        }

        if (request.Email != null)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            if (!string.Equals(normalizedEmail, user.Email, StringComparison.Ordinal))
            {
                var existingByEmail = await _unitOfWork.UserRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
                if (existingByEmail != null && existingByEmail.Id != user.Id)
                {
                    throw new ConflictException($"User with email '{normalizedEmail}' already exists.");
                }

                user.Email = normalizedEmail;
            }
        }

        if (request.Phone != null)
        {
            var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim();

            if (!string.Equals(normalizedPhone, user.Phone, StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    var existingByPhone = await _unitOfWork.UserRepository.GetByPhoneAsync(normalizedPhone, cancellationToken);
                    if (existingByPhone != null && existingByPhone.Id != user.Id)
                    {
                        throw new ConflictException($"User with phone '{normalizedPhone}' already exists.");
                    }
                }

                user.Phone = normalizedPhone;
            }
        }

        if (request.Location != null)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                user.Location = null;
            }
            else
            {
                _ = Enum.TryParse<Location>(request.Location, true, out var parsedLocation);
                user.Location = parsedLocation;
            }
        }

        if (request.About != null)
        {
            user.AboutMe = string.IsNullOrWhiteSpace(request.About)
                ? null
                : request.About.Trim();
        }

        if (request.Avatar != null)
        {
            user.Avatar = string.IsNullOrWhiteSpace(request.Avatar)
                ? null
                : request.Avatar.Trim();
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.UserRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToGetCurrentUserResult();
    }
}
