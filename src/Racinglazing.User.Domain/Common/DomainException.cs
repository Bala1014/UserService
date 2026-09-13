namespace Racinglazing.User.Domain.Common;

/// <summary>
/// Base for expected, business-meaningful failures. Each carries a stable
/// machine-readable <see cref="Code"/> and a human-readable message, which the
/// API surface maps onto the { error: { code, message } } envelope and an HTTP
/// status. Throwing these keeps the use-case code linear — the failure path is
/// an exception, not a branch threaded through every return type.
/// </summary>
public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>The requested resource does not exist. Maps to 404.</summary>
public sealed class NotFoundException(string code, string message) : DomainException(code, message);

/// <summary>The request is well-formed but semantically invalid. Maps to 400.</summary>
public sealed class ValidationFailedException(string code, string message) : DomainException(code, message);

/// <summary>The request conflicts with current state (e.g. a duplicate). Maps to 409.</summary>
public sealed class ConflictException(string code, string message) : DomainException(code, message);

/// <summary>Authentication is missing or invalid. Maps to 401.</summary>
public sealed class UnauthorizedException(string code, string message) : DomainException(code, message);

/// <summary>The caller is authenticated but not permitted. Maps to 403.</summary>
public sealed class ForbiddenException(string code, string message) : DomainException(code, message);
