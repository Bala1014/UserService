namespace Racinglazing.User.Application.Abstractions.Services;

/// <summary>Abstraction over the system clock, so time-dependent logic is testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// The caller's network address, used to annotate issued and revoked refresh
/// tokens for audit. Resolved from the request in the API layer.
/// </summary>
public interface IRequestContext
{
    string? IpAddress { get; }
}
