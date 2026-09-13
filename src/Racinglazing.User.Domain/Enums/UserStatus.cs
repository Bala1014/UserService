namespace Racinglazing.User.Domain.Enums;

/// <summary>
/// Lifecycle state of an account. Authentication is refused for anything other
/// than <see cref="Active"/>, so disabling an account is a data change rather
/// than a deletion — history and foreign references stay intact.
/// </summary>
public enum UserStatus
{
    /// <summary>Normal, able to authenticate.</summary>
    Active = 0,

    /// <summary>Temporarily blocked (e.g. too many failed logins). Can be reopened.</summary>
    Locked = 1,

    /// <summary>Administratively disabled. Cannot authenticate until reinstated.</summary>
    Disabled = 2
}
