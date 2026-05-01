using JobNecto.API.Infrastructure;
using JobNecto.Application.Educations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobNecto.API.Controllers;

/// <summary>
/// Controller for creating user education records.
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
        CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/educations/{result.Id}", result);
    }
}