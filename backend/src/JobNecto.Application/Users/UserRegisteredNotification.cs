using MediatR;

namespace JobNecto.Application.Users;

/// <summary>
/// Published after a new user account has been saved.
/// </summary>
/// <param name="UserId">Id of the newly registered user.</param>
public sealed record UserRegisteredNotification(Guid UserId) : INotification;
