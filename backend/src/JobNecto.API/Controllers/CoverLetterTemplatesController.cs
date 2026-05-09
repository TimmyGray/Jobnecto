using JobNecto.API.Infrastructure;
using JobNecto.Application.CoverLetterTemplates;
using JobNecto.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobNecto.API.Controllers;

/// <summary>
/// Controller for managing cover letter templates.
/// </summary>
[ApiController]
[Route("api/v1/cover-letter-templates")]
[Authorize]
public class CoverLetterTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverLetterTemplatesController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public CoverLetterTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns a cursor-paginated list of cover letter templates belonging to the current authenticated user.
    /// Supports optional case-insensitive name search.
    /// </summary>
    /// <param name="pageSize">Number of items per page (default 20, max 100).</param>
    /// <param name="lastSeenId">Cursor: ID of the last seen template from the previous page.</param>
    /// <param name="lastSeenUpdatedAt">Cursor: UpdatedAt of the last seen template from the previous page.</param>
    /// <param name="search">Optional case-insensitive name filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of cover letter template list-item results.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CoverLetterTemplateListItemResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<CoverLetterTemplateListItemResult>>> ListAsync(
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateTime? lastSeenUpdatedAt = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        if (lastSeenUpdatedAt.HasValue)
        {
            lastSeenUpdatedAt =
                lastSeenUpdatedAt.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(lastSeenUpdatedAt.Value, DateTimeKind.Utc)
                    : lastSeenUpdatedAt.Value.ToUniversalTime();
        }

        var query = new ListCoverLetterTemplatesQuery
        {
            UserId = userId,
            PageSize = pageSize,
            LastSeenId = lastSeenId,
            LastSeenUpdatedAt = lastSeenUpdatedAt,
            Search = search,
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns the full detail of a single cover letter template owned by the current authenticated user.
    /// </summary>
    /// <param name="id">The cover letter template identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cover letter template with full content.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CoverLetterTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverLetterTemplateResult>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new GetCoverLetterTemplateQuery { CoverLetterTemplateId = id, UserId = userId },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Creates a new cover letter template for the current authenticated user.
    /// </summary>
    /// <param name="command">Cover letter template creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created cover letter template.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CoverLetterTemplateResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CoverLetterTemplateResult>> Create(
        CreateCoverLetterTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/cover-letter-templates/{result.Id}", result);
    }
}
