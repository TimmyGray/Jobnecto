using System;

namespace JobNecto.Application.Exceptions;

/// <summary>
/// Exception thrown when a request conflicts with the current state of a resource.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException() : base("A conflict occurred with the current state of the resource.")
    {
    }

    public ConflictException(string message) : base(message)
    {
    }
}
