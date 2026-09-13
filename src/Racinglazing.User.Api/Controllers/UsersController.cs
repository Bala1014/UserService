using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Racinglazing.User.Application.Contracts;
using Racinglazing.User.Application.Features.Users;

namespace Racinglazing.User.Api.Controllers;

[Route("api/v1/users")]
public sealed class UsersController(IUserService users) : ApiControllerBase
{
    /// <summary>Returns the authenticated user's own profile.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Data(await users.GetCurrentAsync(ct));

    /// <summary>Updates the authenticated user's profile.</summary>
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest body, CancellationToken ct)
        => Data(await users.UpdateCurrentAsync(body, ct));

    /// <summary>
    /// Returns a single user's public reference (id, username, display name,
    /// avatar). Used by the other services to render author details.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken ct)
        => Data(await users.GetPublicAsync(userId, ct));

    /// <summary>
    /// Batch public-reference lookup, keyed by id. This is the endpoint
    /// ForumService's <c>IUserProfileProvider</c> (and RaceService) call to turn
    /// stored author ids into display data in a single round trip.
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> Batch([FromBody] BatchUsersRequest body, CancellationToken ct)
        => Data(await users.GetManyAsync(body.Ids, ct));
}

/// <summary>Ids to resolve in a batch lookup.</summary>
public sealed record BatchUsersRequest(IReadOnlyList<Guid> Ids);
