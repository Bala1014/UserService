using Microsoft.AspNetCore.Mvc;
using Racinglazing.User.Application.Contracts;
using Racinglazing.User.Application.Features.Authentication;

namespace Racinglazing.User.Api.Controllers;

/// <summary>
/// Credential and external sign-in. These are the only unauthenticated write
/// endpoints in the platform — everything else verifies a token minted here.
/// </summary>
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Registers a new account and returns an initial token pair.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest body, CancellationToken ct)
        => Data(await auth.RegisterAsync(body, ct));

    /// <summary>Signs in with an email/username and password.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest body, CancellationToken ct)
        => Data(await auth.LoginAsync(body, ct));

    /// <summary>Exchanges a valid refresh token for a new token pair (rotating it).</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest body, CancellationToken ct)
        => Data(await auth.RefreshAsync(body, ct));

    /// <summary>Revokes a refresh token (sign-out). Idempotent.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest body, CancellationToken ct)
    {
        await auth.LogoutAsync(body, ct);
        return NoContent();
    }

    /// <summary>Signs in (or provisions) a user from a Google OIDC id_token.</summary>
    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleLoginRequest body, CancellationToken ct)
        => Data(await auth.GoogleLoginAsync(body, ct));
}
