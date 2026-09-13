namespace Racinglazing.User.Application.Contracts;

/// <summary>Full profile of the authenticated user (the "/me" shape).</summary>
public sealed record UserDto(
    string Id,
    string Email,
    bool EmailVerified,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> Roles,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

/// <summary>
/// Lightweight public representation of a user, returned to the other services
/// for display enrichment. This is the exact shape ForumService's
/// <c>IUserProfileProvider</c> expects (id, username, displayName, avatarUrl).
/// </summary>
public sealed record UserRefDto(
    string Id,
    string Username,
    string DisplayName,
    string? AvatarUrl);

/// <summary>Profile edit payload for the authenticated user.</summary>
public sealed record UpdateProfileRequest(
    string DisplayName,
    string? AvatarUrl,
    string? Bio);
