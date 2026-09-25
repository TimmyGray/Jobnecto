using FluentAssertions;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using JobNecto.Infrastructure.Services;
using Moq;

namespace JobNecto.Tests.Application.Users;

public class SignInCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly SignInCommandHandler _handler;

    public SignInCommandHandlerTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);
        _handler = new SignInCommandHandler(_uowMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_ResolvesByEmail_LowercasedLookup_Succeeds()
    {
        var command = new SignInCommand { Identifier = "Daria@Example.com", Password = "Password123!" };
        var user = new User { Id = Guid.NewGuid(), Email = "daria@example.com", Login = "DariaDev", Password = "hashed" };

        _userRepoMock.Setup(x => x.GetByEmailAsync("daria@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user.Password, command.Password)).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        _userRepoMock.Verify(x => x.GetByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailLookupMisses_ResolvesByLogin_Succeeds()
    {
        var command = new SignInCommand { Identifier = "daria_dev", Password = "Password123!" };
        var user = new User { Id = Guid.NewGuid(), Email = "daria@example.com", Login = "daria_dev", Password = "hashed" };

        _userRepoMock.Setup(x => x.GetByEmailAsync("daria_dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync("daria_dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user.Password, command.Password)).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.LoginName.Should().Be(user.Login);
    }

    [Fact]
    public async Task Handle_MixedCaseLogin_Resolves()
    {
        // Regression guard for Trap 2: the login lookup must pass the identifier unmodified in case.
        var command = new SignInCommand { Identifier = "DariaDev", Password = "Password123!" };
        var user = new User { Id = Guid.NewGuid(), Email = "daria@example.com", Login = "DariaDev", Password = "hashed" };

        _userRepoMock.Setup(x => x.GetByEmailAsync("dariadev", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync("DariaDev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user.Password, command.Password)).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(user.Id);
        // Proves the login lookup was NOT lowercased before hitting the repository.
        _userRepoMock.Verify(x => x.GetByLoginAsync("DariaDev", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownIdentifier_ThrowsInvalidCredentialsException()
    {
        var command = new SignInCommand { Identifier = "ghost", Password = "Password123!" };

        _userRepoMock.Setup(x => x.GetByEmailAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(It.IsAny<string>(), command.Password)).Returns(false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var command = new SignInCommand { Identifier = "daria_dev", Password = "WrongPassword!" };
        var user = new User { Id = Guid.NewGuid(), Email = "daria@example.com", Login = "daria_dev", Password = "hashed" };

        _userRepoMock.Setup(x => x.GetByEmailAsync("daria_dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync("daria_dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user.Password, command.Password)).Returns(false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Handle_UnknownIdentifier_StillInvokesVerifyHashedPassword_AgainstDummyHash()
    {
        // Regression guard for Trap 3: the not-found path must not short-circuit before hashing,
        // otherwise response timing discloses account existence.
        var command = new SignInCommand { Identifier = "ghost", Password = "Password123!" };

        _userRepoMock.Setup(x => x.GetByEmailAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(x => x.GetByLoginAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(It.IsAny<string>(), command.Password)).Returns(false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _passwordHasherMock.Verify(
            x => x.VerifyHashedPassword(It.IsAny<string>(), command.Password),
            Times.Once);
    }

    [Fact]
    public void DummyPasswordHash_IsAValidlyFormattedPbkdf2HashThatMatchesNoRealPassword()
    {
        // The dummy hash burns real PBKDF2 iterations on the not-found path (Trap 3), so it must be
        // a validly-formatted hash the real hasher accepts, not a placeholder string that short-circuits.
        var realHasher = new Pbkdf2PasswordHasher();

        realHasher.IsHashSupportedFormat(SignInCommandHandler.DummyPasswordHash).Should().BeTrue();
        realHasher.VerifyHashedPassword(SignInCommandHandler.DummyPasswordHash, "any-password-a-caller-might-send").Should().BeFalse();
    }
}
