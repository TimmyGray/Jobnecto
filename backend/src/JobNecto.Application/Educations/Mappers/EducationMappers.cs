using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Educations.Mappers;

/// <summary>
/// Mapping extensions for Education create flow.
/// </summary>
public static class EducationMappers
{
    /// <summary>
    /// Maps a create command into a domain entity.
    /// </summary>
    /// <param name="command">Incoming command payload.</param>
    /// <returns>Mapped education entity.</returns>
    public static Education ToEntity(this CreateEducationCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (!Enum.TryParse<Degree>(command.Degree, true, out var parsedDegree))
            throw new InvalidOperationException($"Invalid degree value '{command.Degree}'. This should have been caught by validation.");

        return new Education
        {
            UserId = command.UserId,
            Title = command.Title,
            Specialization = command.Specialization,
            Degree = parsedDegree,
        };
    }

    /// <summary>
    /// Maps a domain education entity to response DTO.
    /// </summary>
    /// <param name="education">Persisted education entity.</param>
    /// <returns>API-facing education result.</returns>
    public static EducationResult ToEducationResult(this Education education)
    {
        if (education == null)
            throw new ArgumentNullException(nameof(education));

        return new EducationResult
        {
            Id = education.Id,
            UserId = education.UserId,
            Title = education.Title,
            Specialization = education.Specialization,
            Degree = education.Degree.ToString().ToLowerInvariant(),
            CreatedAt = education.CreatedAt,
            UpdatedAt = education.UpdatedAt,
        };
    }
}