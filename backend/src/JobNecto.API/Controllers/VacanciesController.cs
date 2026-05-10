using JobNecto.API.Infrastructure;
using JobNecto.Application.Vacancies;
using JobNecto.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobNecto.API.Controllers;

/// <summary>
/// Controller for vacancy browse and filter endpoints.
/// </summary>
[ApiController]
[Route("api/v1/vacancies")]
[Authorize]
public class VacanciesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="VacanciesController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public VacanciesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns a cursor-paginated vacancy list. Empty criteria represents browse mode.
    /// </summary>
    /// <param name="query">Vacancy filter payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated vacancy list in the shared envelope format.</returns>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(PagedResult<VacancyListItemResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<VacancyListItemResult>>> FilterAsync(
        [FromBody] FilterVacanciesQuery? query,
        CancellationToken cancellationToken = default
    )
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        query ??= new FilterVacanciesQuery();
        query.UserId = userId;

        if (query.LastSeenUpdatedAt.HasValue)
        {
            query.LastSeenUpdatedAt = query.LastSeenUpdatedAt.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(query.LastSeenUpdatedAt.Value, DateTimeKind.Utc)
                : query.LastSeenUpdatedAt.Value.ToUniversalTime();
        }

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}