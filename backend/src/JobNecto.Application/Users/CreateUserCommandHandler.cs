using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users.Mappers;
using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Handler for creating a new user account.
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandHandler"/> class.
    /// </summary>
    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Handles the creation of a new user.
    /// </summary>
    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Normalize once so that uniqueness checks and entity creation use the same values.
        request.Email = request.Email.Trim().ToLowerInvariant();
        request.LoginName = request.LoginName.Trim();

        // Check for uniqueness
        var existingByEmail = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingByEmail != null)
        {
            throw new ConflictException($"User with email '{request.Email}' already exists.");
        }

        var existingByLogin = await _unitOfWork.UserRepository.GetByLoginAsync(request.LoginName, cancellationToken);
        if (existingByLogin != null)
        {
            throw new ConflictException($"User with login '{request.LoginName}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            request.Phone = request.Phone.Trim();

            var existingByPhone = await _unitOfWork.UserRepository.GetByPhoneAsync(request.Phone, cancellationToken);
            if (existingByPhone != null)
            {
                throw new ConflictException($"User with phone '{request.Phone}' already exists.");
            }
        }

        // Hash password before mapping to persistence entity.
        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        // Create entity
        var user = request.ToEntity(hashedPassword);

        // Add and save
        await _unitOfWork.UserRepository.CreateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return result
        return user.ToCreateUserResult();
    }
}