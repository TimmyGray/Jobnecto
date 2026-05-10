using JobNecto.Domain.Entities;

namespace JobNecto.Application.CoverLetters.Mappers;

/// <summary>
/// Mapping extensions for cover letter command/query flows.
/// </summary>
public static class CoverLetterMappers
{
    /// <summary>
    /// Maps a create command into a domain entity.
    /// </summary>
    /// <param name="command">Incoming command payload.</param>
    /// <returns>Mapped cover letter entity.</returns>
    public static CoverLetter ToEntity(this CreateCoverLetterCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        return new CoverLetter
        {
            UserId = command.UserId,
            VacancyId = command.VacancyId,
            Content = command.Content,
        };
    }

    /// <summary>
    /// Maps a domain entity to the create response DTO.
    /// </summary>
    /// <param name="coverLetter">Persisted cover letter entity.</param>
    /// <returns>API-facing create cover letter result.</returns>
    public static CreateCoverLetterResult ToCreateResult(this CoverLetter coverLetter)
    {
        if (coverLetter == null)
            throw new ArgumentNullException(nameof(coverLetter));

        return new CreateCoverLetterResult
        {
            Id = coverLetter.Id,
            VacancyId = coverLetter.VacancyId,
            Content = coverLetter.Content,
            CreatedAt = coverLetter.CreatedAt,
            UpdatedAt = coverLetter.UpdatedAt,
        };
    }

    /// <summary>
    /// Maps a domain entity to the update response DTO.
    /// </summary>
    /// <param name="coverLetter">Updated cover letter entity.</param>
    /// <returns>API-facing update cover letter result.</returns>
    public static CoverLetterUpdateResult ToUpdateResult(this CoverLetter coverLetter)
    {
        if (coverLetter == null)
            throw new ArgumentNullException(nameof(coverLetter));

        return new CoverLetterUpdateResult
        {
            Id = coverLetter.Id,
            VacancyId = coverLetter.VacancyId,
            Content = coverLetter.Content,
            CreatedAt = coverLetter.CreatedAt,
            UpdatedAt = coverLetter.UpdatedAt,
        };
    }
}