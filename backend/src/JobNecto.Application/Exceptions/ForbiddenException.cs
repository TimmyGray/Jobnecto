using System;

namespace JobNecto.Application.Exceptions;

/// <summary>
/// Exception thrown when an action is forbidden for the current user.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException() : base()
    {
    }
    
    public ForbiddenException(string message) : base(message)
    {
    }
}
