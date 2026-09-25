namespace JobNecto.Application.Exceptions;

/// <summary>
/// Exception thrown by <see cref="Users.SignInCommandHandler"/> when sign-in credentials do not resolve
/// to an active user. This is a handler-internal failure signal only: it is caught in the controller
/// and never reaches the API layer's global exception handler.
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid credentials.")
    {
    }

    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
