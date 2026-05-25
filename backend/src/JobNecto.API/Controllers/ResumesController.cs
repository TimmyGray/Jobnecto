using JobNecto.API.Infrastructure;
using JobNecto.Application.Resumes;
using JobNecto.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobNecto.API.Controllers;

/// <summary>
/// Controller for managing user resumes.
/// </summary>
[ApiController]
[Route("api/v1/resumes")]
[Authorize]
public class ResumesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResumesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new resume for the current authenticated user.
    /// </summary>
    /// <param name="command">Resume creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created resume detail.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ResumeResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResumeResult>> Create(
        CreateResumeCommand command, 
        CancellationToken cancellationToken)
    {
        // 1. Extract current UserId from JWT context
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        // 2. Assign UserId to command (owner identification)
        command.UserId = userId;

        // 3. Dispatch via MediatR
        var result = await _mediator.Send(command, cancellationToken);

        // 4. Return 201 Created with Location header
        // Following the API pattern: /api/v1/resumes/{id}
        return Created($"/api/v1/resumes/{result.Id}", result);
    }

    /// <summary>
    /// Returns a cursor-paginated list of resumes belonging to the current authenticated user.
    /// </summary>
    /// <param name="pageSize">Number of items per page (default 20, max 100).</param>
    /// <param name="lastSeenId">Cursor: ID of the last seen resume from the previous page.</param>
    /// <param name="lastSeenUpdatedAt">Cursor: UpdatedAt of the last seen resume from the previous page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of resumes for the current user.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ResumeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<ResumeResult>>> ListAsync(
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateTime? lastSeenUpdatedAt = null,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        if (lastSeenUpdatedAt.HasValue)
        {
            lastSeenUpdatedAt = lastSeenUpdatedAt.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(lastSeenUpdatedAt.Value, DateTimeKind.Utc)
                : lastSeenUpdatedAt.Value.ToUniversalTime();
        }

        var query = new ListResumesQuery
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
    /// Returns the full detail of a specific resume owned by the current authenticated user.
    /// </summary>
    /// <param name="id">The resume ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resume detail, or 404 if not found or not owned by the caller.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResumeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeResult>> GetAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var query = new GetResumeQuery { ResumeId = id, UserId = userId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates one or more fields of an existing resume owned by the current authenticated user.
    /// </summary>
    /// <param name="id">The resume ID.</param>
    /// <param name="command">Resume update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated resume detail.</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ResumeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeResult>> UpdateAsync(
        [FromRoute] Guid id,
        UpdateResumeCommand command,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.ResumeId = id;
        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes an existing resume owned by the current authenticated user.
    /// </summary>
    /// <param name="id">The resume ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content when delete succeeds.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var command = new DeleteResumeCommand
        {
            ResumeId = id,
            UserId = userId,
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
