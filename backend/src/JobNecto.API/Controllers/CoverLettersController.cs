using JobNecto.API.Infrastructure;
using JobNecto.Application.CoverLetters;
using JobNecto.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobNecto.API.Controllers;

/// <summary>
/// Controller for managing cover letters.
/// </summary>
[ApiController]
[Route("api/v1/cover-letters")]
[Authorize]
public class CoverLettersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverLettersController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public CoverLettersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns detail data for a single cover letter belonging to the authenticated user.
    /// </summary>
    /// <param name="id">Cover letter identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cover letter detail response.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CoverLetterDetailResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverLetterDetailResult>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var query = new GetCoverLetterQuery
        {
            CoverLetterId = id,
            UserId = userId,
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a cursor-paginated list of cover letters belonging to the current authenticated user.
    /// Ordered by CreatedAt descending.
    /// </summary>
    /// <param name="pageSize">Number of items per page (default 20, max 100).</param>
    /// <param name="lastSeenId">Cursor: ID of the last seen cover letter from the previous page.</param>
    /// <param name="lastSeenUpdatedAt">Cursor timestamp field carrying CreatedAt for this endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of cover letter list-item results.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CoverLetterListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<CoverLetterListItem>>> ListAsync(
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateTime? lastSeenUpdatedAt = null,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        if (lastSeenId.HasValue ^ lastSeenUpdatedAt.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "lastSeenId and lastSeenUpdatedAt must both be provided or both omitted.",
            });
        }

        if (lastSeenUpdatedAt.HasValue)
        {
            lastSeenUpdatedAt =
                lastSeenUpdatedAt.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(lastSeenUpdatedAt.Value, DateTimeKind.Utc)
                    : lastSeenUpdatedAt.Value.ToUniversalTime();
        }

        var query = new ListCoverLettersQuery
        {
            UserId = userId,
            PageSize = pageSize,
            LastSeenId = lastSeenId,
            LastSeenUpdatedAt = lastSeenUpdatedAt,
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new cover letter for a vacancy owned by the current authenticated user.
    /// </summary>
    /// <param name="command">Create cover letter payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created cover letter.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateCoverLetterResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateCoverLetterResult>> CreateAsync(
        CreateCoverLetterCommand command,
        CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/cover-letters/{result.Id}", result);
    }

    /// <summary>
    /// Updates content of an existing cover letter owned by the authenticated user.
    /// </summary>
    /// <param name="id">Cover letter identifier.</param>
    /// <param name="command">Update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated cover letter result.</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(CoverLetterUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverLetterUpdateResult>> UpdateAsync(
        Guid id,
        UpdateCoverLetterCommand command,
        CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.CoverLetterId = id;
        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes an existing cover letter owned by the authenticated user.
    /// </summary>
    /// <param name="id">Cover letter identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on successful deletion.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var command = new DeleteCoverLetterCommand
        {
            CoverLetterId = id,
            UserId = userId,
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}