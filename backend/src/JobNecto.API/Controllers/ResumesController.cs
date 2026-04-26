using JobNecto.API.Infrastructure;
using JobNecto.Application.Resumes;
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
}
