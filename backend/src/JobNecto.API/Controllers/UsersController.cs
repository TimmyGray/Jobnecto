using JobNecto.Application.Users;
using JobNecto.Application.Interfaces;
using JobNecto.API.Contracts.Auth;
using JobNecto.API.Contracts.Users;
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
    private const long MaxAvatarSizeBytes = 5L * 1024 * 1024;

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

    /// <summary>
    /// Retrieves the current authenticated user's profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current user profile.</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(GetCurrentUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCurrentUserResult>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetCurrentUserQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the current authenticated user's profile.
    /// </summary>
    /// <param name="command">Update profile command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile.</returns>
    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(typeof(GetCurrentUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GetCurrentUserResult>> UpdateCurrentUser(
        UpdateCurrentUserCommand command,
        CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        command.UserId = userId;

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Uploads a new avatar for the current authenticated user.
    /// If an avatar exists, it is replaced.
    /// </summary>
    /// <param name="request">Multipart form request containing avatar file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile.</returns>
    [HttpPost("me/avatar")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAvatarSizeBytes)]
    [RequestSizeLimit(MaxAvatarSizeBytes)]
    [ProducesResponseType(typeof(GetCurrentUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCurrentUserResult>> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        return await UpsertAvatarInternal(request, cancellationToken);
    }

    /// <summary>
    /// Replaces the current authenticated user's avatar.
    /// </summary>
    /// <param name="request">Multipart form request containing avatar file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile.</returns>
    [HttpPut("me/avatar")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAvatarSizeBytes)]
    [RequestSizeLimit(MaxAvatarSizeBytes)]
    [ProducesResponseType(typeof(GetCurrentUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCurrentUserResult>> UpdateAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        return await UpsertAvatarInternal(request, cancellationToken);
    }

    /// <summary>
    /// Deletes current authenticated user's avatar.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user profile with <c>avatar</c> set to null.</returns>
    [HttpDelete("me/avatar")]
    [Authorize]
    [ProducesResponseType(typeof(GetCurrentUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCurrentUserResult>> DeleteAvatar(CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new DeleteCurrentUserAvatarCommand { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Issues a renewed JWT for authenticated clients.
    /// Browser clients continue to receive the token via HTTP-only cookie; non-browser clients
    /// can consume the returned token body and send it in <c>Authorization: Bearer</c>.
    /// The response body token is returned only when the request used bearer transport.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Renewed access token contract.</returns>
    [HttpPost("token/refresh")]
    [Authorize]
    [ProducesResponseType(typeof(RefreshAccessTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshAccessTokenResult>> RefreshToken(CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var token = await _jwtService.GenerateTokenAsync(userId);
        _cookieAuthService.SetAuthCookie(Response, token);

        return Ok(new RefreshAccessTokenResult
        {
            AccessToken = UsesBearerTransport(Request) ? token : string.Empty
        });
    }

    private static bool UsesBearerTransport(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return false;
        }

        return authorizationHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ActionResult<GetCurrentUserResult>> UpsertAvatarInternal(
        UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        if (request.Avatar == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "Avatar file is required."
            });
        }

        if (request.Avatar.Length <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "Avatar file is required."
            });
        }

        if (request.Avatar.Length > MaxAvatarSizeBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "Avatar file size must be less than or equal to 5 MB."
            });
        }

        var fileName = string.IsNullOrWhiteSpace(request.Avatar.FileName)
            ? "avatar"
            : request.Avatar.FileName;
        var contentType = request.Avatar.ContentType?.Trim() ?? string.Empty;

        await using var avatarStream = request.Avatar.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await avatarStream.CopyToAsync(memoryStream, cancellationToken);

        var command = new UploadCurrentUserAvatarCommand
        {
            UserId = userId,
            FileName = fileName,
            ContentType = contentType,
            Content = memoryStream.ToArray()
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
