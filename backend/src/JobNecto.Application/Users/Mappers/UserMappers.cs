using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Users.Mappers;

/// <summary>
/// Mapping extension methods for User entity and related DTOs.
/// Follows the pattern: Command/Request -> Entity, Entity -> Result/Response.
/// This pattern can be replicated for Resume, Education, CoverLetter, etc.
/// </summary>
public static class UserMappers
{
    /// <summary>
    /// Maps a CreateUserCommand (request DTO) to a User domain entity.
    /// Handles field name mapping: loginName -> Login, about -> AboutMe, location string -> Location enum.
    /// </summary>
    public static User ToEntity(this CreateUserCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        // Parse location enum if provided; set only when parsing succeeds, otherwise keep null.
        Location? location = null;
        if (!string.IsNullOrWhiteSpace(command.Location))
        {
            if (Enum.TryParse<Location>(command.Location, ignoreCase: true, out var parsedLocation))
            {
                location = parsedLocation;
            }
        }

        return new User
        {
            Login = command.LoginName,
            Email = command.Email,
            Password = command.Password,
            Phone = command.Phone,
            Location = location,
            AboutMe = command.About,
            Avatar = command.Avatar
        };
    }

    /// <summary>
    /// Maps a User domain entity to a CreateUserResult (response DTO).
    /// Excludes sensitive fields (Password, Id if needed for external responses).
    /// Handles field name mapping: Login -> LoginName, AboutMe -> About, Location enum -> string.
    /// </summary>
    public static CreateUserResult ToCreateUserResult(this User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        return new CreateUserResult
        {
            Id = user.Id,
            LoginName = user.Login,
            Email = user.Email,
            Phone = user.Phone,
            Location = user.Location?.ToString(),
            About = user.AboutMe,
            Avatar = user.Avatar
        };
    }
}
