using JobNecto.API.Infrastructure;
using JobNecto.Application.CoverLetterTemplates;
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
