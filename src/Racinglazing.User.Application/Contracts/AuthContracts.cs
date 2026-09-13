namespace Racinglazing.User.Application.Contracts;

// ----- Requests -----

/// <summary>Sign-up payload. Password confirmation is validated, never stored.</summary>
public sealed record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string ConfirmPassword);

/// <summary>Credential login. The identifier may be either the email or the username.</summary>
public sealed record LoginRequest(
    string EmailOrUsername,
    string Password);

/// <summary>Exchanges a valid refresh token for a fresh token pair.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>Revokes a refresh token (sign-out of the current session).</summary>
public sealed record LogoutRequest(string RefreshToken);

/// <summary>Signs in (or provisions) a user from a Google OIDC id_token.</summary>
public sealed record GoogleLoginRequest(string IdToken);

// ----- Responses -----

/// <summary>
/// The result of any successful authentication: a short-lived access token to
/// send as a Bearer on the other services, and a long-lived refresh token to
/// obtain the next access token. <paramref name="User"/> is included so a client
/// need not make a second call to render the signed-in state.
/// </summary>
public sealed record AuthResultDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);
