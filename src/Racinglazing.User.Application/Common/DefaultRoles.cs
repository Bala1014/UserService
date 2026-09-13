namespace Racinglazing.User.Application.Common;

/// <summary>
/// Role names UserService knows about. The baseline <see cref="User"/> role is
/// granted to every new account; the rest are seeded so they can be assigned by
/// an administrator. The names deliberately match what the consuming services
/// look for in the <c>role</c> claim, so an assignment here is immediately
/// meaningful there.
/// </summary>
public static class DefaultRoles
{
    /// <summary>Baseline role granted on registration.</summary>
    public const string User = "User";

    /// <summary>Platform-wide administrator.</summary>
    public const string Administrator = "Administrator";

    // ForumService vocabulary.
    public const string Moderator = "Moderator";

    // RaceService vocabulary.
    public const string Editor = "Editor";
    public const string Entrant = "Entrant";

    /// <summary>Every role seeded at startup, with a short description.</summary>
    public static IReadOnlyList<(string Name, string Description)> All { get; } =
    [
        (User, "Baseline authenticated user."),
        (Administrator, "Platform administrator with full privileges."),
        (Moderator, "ForumService moderator."),
        (Editor, "RaceService content editor."),
        (Entrant, "RaceService entrant.")
    ];
}
