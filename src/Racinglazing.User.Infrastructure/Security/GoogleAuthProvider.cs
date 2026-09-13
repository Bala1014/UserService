using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Racinglazing.User.Application.Abstractions.Security;

namespace Racinglazing.User.Infrastructure.Security;

/// <summary>Google sign-in settings, bound from the "Google" configuration section.</summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Google";

    /// <summary>The OAuth client id the web app uses. The id_token's <c>aud</c> must match it.</summary>
    public string? ClientId { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId);
}

/// <summary>
/// Verifies a Google-issued OIDC <c>id_token</c> using Google's published keys
/// (fetched and cached by the Google library), and projects it onto
/// <see cref="ExternalUserInfo"/>. A failed or untrusted token yields null — the
/// use case turns that into a 401.
/// </summary>
public sealed class GoogleAuthProvider(
    IOptions<GoogleAuthOptions> options,
    ILogger<GoogleAuthProvider> logger) : IExternalAuthProvider
{
    private readonly GoogleAuthOptions _options = options.Value;

    public string Provider => "google";

    public async Task<ExternalUserInfo?> ValidateAsync(string credential, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            logger.LogWarning("Google sign-in attempted but Google:ClientId is not configured.");
            return null;
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_options.ClientId!]
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

            if (string.IsNullOrWhiteSpace(payload.Email))
                return null;

            return new ExternalUserInfo(
                Provider,
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogInformation(ex, "Rejected an invalid Google id_token.");
            return null;
        }
    }
}
