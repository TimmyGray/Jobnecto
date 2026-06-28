using FluentAssertions;
using JobNecto.Application.Exceptions;

namespace JobNecto.Tests.Application;

public class ApplicationExceptionsTests
{
    [Fact]
    public void ConflictException_Parameterless_UsesDefaultMessage()
    {
        var ex = new ConflictException();

        ex.Message.Should().Be("A conflict occurred with the current state of the resource.");
    }

    [Fact]
    public void ConflictException_WithMessage_UsesProvidedMessage()
    {
        var ex = new ConflictException("duplicate email");

        ex.Message.Should().Be("duplicate email");
    }

    [Fact]
    public void UnauthorizedException_Parameterless_CanBeConstructed()
    {
        var ex = new UnauthorizedException();

        ex.Should().BeAssignableTo<Exception>();
        ex.Message.Should().NotBeNull();
    }

    [Fact]
    public void UnauthorizedException_WithMessage_UsesProvidedMessage()
    {
        var ex = new UnauthorizedException("invalid credentials");

        ex.Message.Should().Be("invalid credentials");
    }
}
