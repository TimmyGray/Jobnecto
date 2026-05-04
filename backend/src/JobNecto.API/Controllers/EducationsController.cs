using JobNecto.API.Infrastructure;
using JobNecto.Application.Educations;
using JobNecto.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobNecto.API.Controllers;

/// <summary>
/// Controller for managing user education records.
/// </summary>
[ApiController]
[Route("api/v1/educations")]
[Authorize]
public class EducationsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EducationsController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public EducationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new education record for the current authenticated user.
    /// </summary>
    /// <param name="command">Education creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created education record.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EducationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EducationResult>> Create(
        CreateEducationCommand command,
        CancellationToken cancellationToken
    )
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/educations/{result.Id}", result);
    }

    /// <summary>
    /// Returns a cursor-paginated list of education records belonging to the current authenticated user.
    /// </summary>
    /// <param name="pageSize">Number of items per page (default 20, max 100).</param>
    /// <param name="lastSeenId">Cursor: ID of the last seen education record from the previous page.</param>
    /// <param name="lastSeenUpdatedAt">Cursor: UpdatedAt of the last seen education record from the previous page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of education records for the current user.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EducationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<EducationResult>>> ListAsync(
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateTime? lastSeenUpdatedAt = null,
        CancellationToken cancellationToken = default
    )
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

        var query = new ListEducationsQuery
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
    /// Returns the full detail of a specific education record owned by the current authenticated user.
    /// </summary>
    /// <param name="id">The education record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The education record detail, or 404 if not found or not owned by the caller.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EducationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EducationResult>> GetAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var query = new GetEducationQuery { EducationId = id, UserId = userId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates one or more fields of an existing education record owned by the current authenticated user.
    /// </summary>
    /// <param name="id">The education record ID.</param>
    /// <param name="command">Education update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated education record.</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(EducationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EducationResult>> UpdateAsync(
        [FromRoute] Guid id,
        UpdateEducationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.EducationId = id;
        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes an existing education record owned by the current authenticated user.
    /// </summary>
    /// <param name="id">The education record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content when delete succeeds.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var command = new DeleteEducationCommand { EducationId = id, UserId = userId };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
