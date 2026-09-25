using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users.Mappers;
using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Handler for authenticating a returning user by email or login identifier.
/// </summary>
public class SignInCommandHandler : IRequestHandler<SignInCommand, SignInResult>
{
    /// <summary>
    /// A constant, validly-formatted PBKDF2 hash that matches no real password. Verified against on the
    /// not-found path so response timing does not disclose whether an account exists (Trap 3).
    /// </summary>
    public const string DummyPasswordHash =
        "pbkdf2-sha256$100000$AAECAwQFBgcICQoLDA0ODw==$B4cStVV1JkyKk2UQW7mBhM443S94rEf9DmSl9DvOCyU=";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignInCommandHandler"/> class.
    /// </summary>
    public SignInCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Resolves the identifier to a user (email lookup lowercased, login lookup case-preserving — see
    /// Trap 2), verifies the password with constant-time timing regardless of whether the user was
    /// found (Trap 3), and throws <see cref="InvalidCredentialsException"/> on any failure.
    /// </summary>
    public async Task<SignInResult> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();

        var user = await _unitOfWork.UserRepository.GetByEmailAsync(identifier.ToLowerInvariant(), cancellationToken);
        user ??= await _unitOfWork.UserRepository.GetByLoginAsync(identifier, cancellationToken);

        if (user is null)
        {
            _passwordHasher.VerifyHashedPassword(DummyPasswordHash, request.Password);
            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.VerifyHashedPassword(user.Password, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        return user.ToSignInResult();
    }
}
