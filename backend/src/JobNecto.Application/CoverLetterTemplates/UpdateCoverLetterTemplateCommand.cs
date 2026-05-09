using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Command for updating one or more fields on an existing cover letter template.
/// </summary>
public class UpdateCoverLetterTemplateCommand : IRequest<CoverLetterTemplateResult>
{
    /// <summary>
    /// Cover letter template identifier from route.
    /// </summary>
    [JsonIgnore]
    public Guid CoverLetterTemplateId { get; set; }

    /// <summary>
    /// Authenticated user identifier from security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Updated template name. Null means no change.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Updated template content. Null means no change.
    /// </summary>
    public string? Content { get; set; }
}