using JobNecto.Application.Users;
using JobNecto.Application.Interfaces;
using JobNecto.API.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobNecto.API.Controllers;

/// <summary>
/// Controller for user-related operations.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwtService;
    private readonly ICookieAuthService _cookieAuthService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    public UsersController(IMediator mediator, IJwtTokenService jwtService, ICookieAuthService cookieAuthService)
    {
        _mediator = mediator;
        _jwtService = jwtService;
        _cookieAuthService = cookieAuthService;
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="command">The create user command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user result.</returns>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreateUserResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserResult>> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        // Generate JWT token
        var token = await _jwtService.GenerateTokenAsync(result.Id.ToString());

        // Set HTTP-Only cookie via service
        _cookieAuthService.SetAuthCookie(Response, token);

        // Return created response with Location header
        return Created("/api/v1/users/me", result);
    }
}