namespace Racinglazing.User.Application.Abstractions.Identity;

/// <summary>
/// The caller of the current request, resolved from the validated access token.
/// UserService issues tokens, but it also consumes its own for the authenticated
/// "/me" endpoints — this is how a use case learns who is asking.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The authenticated user's id, or null when anonymous.</summary>
    Guid? UserId { get; }

    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Returns the user id or throws when the request is unauthenticated.</summary>
    Guid RequireUserId();
}
