using JobNecto.Domain.Entities;

namespace JobNecto.Application.CoverLetterTemplates.Mappers;

/// <summary>
/// Mapping extensions for the cover letter template create flow.
/// </summary>
public static class CoverLetterTemplateMappers
{
    /// <summary>
    /// Maps a create command into a domain entity.
    /// </summary>
    /// <param name="command">Incoming command payload.</param>
    /// <returns>Mapped cover letter template entity.</returns>
    public static CoverLetterTemplate ToEntity(this CreateCoverLetterTemplateCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        return new CoverLetterTemplate
        {
            UserId = command.UserId,
            Name = command.Name,
            Content = command.Content,
        };
    }

    /// <summary>
    /// Maps a domain entity to the API response DTO.
    /// </summary>
    /// <param name="template">Persisted cover letter template entity.</param>
    /// <returns>API-facing cover letter template result.</returns>
    public static CoverLetterTemplateResult ToCoverLetterTemplateResult(this CoverLetterTemplate template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        return new CoverLetterTemplateResult
        {
            Id = template.Id,
            UserId = template.UserId,
            Name = template.Name,
            Content = template.Content,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
        };
    }

    /// <summary>
    /// Maps a domain entity to the list-item API response DTO.
    /// Returns a content preview (first 200 chars) instead of the full content.
    /// </summary>
    /// <param name="template">Persisted cover letter template entity.</param>
    /// <returns>API-facing cover letter template list item result.</returns>
    public static CoverLetterTemplateListItemResult ToCoverLetterTemplateListItemResult(
        this CoverLetterTemplate template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        return new CoverLetterTemplateListItemResult
        {
            Id = template.Id,
            Name = template.Name,
            ContentPreview = template.Content.Length <= 200
                ? template.Content
                : template.Content[..200],
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
        };
    }
}
