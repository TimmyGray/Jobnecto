using System;

namespace JobNecto.Application.Exceptions;

/// <summary>
/// Exception thrown when a user is not authorized to access a resource.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base()
    {
    }
    
    public UnauthorizedException(string message) : base(message)
    {
    }
}
