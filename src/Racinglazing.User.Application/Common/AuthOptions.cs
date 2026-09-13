namespace Racinglazing.User.Application.Common;

/// <summary>
/// Authentication policy knobs, bound from the "Auth" configuration section.
/// Kept in the application layer because they govern use-case behaviour, not
/// wiring.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>How long a refresh token remains valid. Default 14 days.</summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    /// <summary>
    /// When true, a Google sign-in for an unknown email provisions a new account
    /// automatically. When false, the user must already exist (or register first).
    /// </summary>
    public bool ExternalAutoProvision { get; set; } = true;
}
