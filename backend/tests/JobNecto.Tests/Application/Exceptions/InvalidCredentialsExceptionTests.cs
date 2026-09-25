using FluentAssertions;
using JobNecto.Application.Exceptions;

namespace JobNecto.Tests.Application.Exceptions;

public class InvalidCredentialsExceptionTests
{
    [Fact]
    public void DefaultConstructor_UsesGenericMessage()
    {
        var exception = new InvalidCredentialsException();

        exception.Message.Should().Be("Invalid credentials.");
    }

    [Fact]
    public void MessageConstructor_UsesProvidedMessage()
    {
        var exception = new InvalidCredentialsException("custom message");

        exception.Message.Should().Be("custom message");
    }
}
