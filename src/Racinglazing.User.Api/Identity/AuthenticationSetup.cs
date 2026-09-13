using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Racinglazing.User.Infrastructure.Security;

namespace Racinglazing.User.Api.Identity;

/// <summary>
/// Validates the access tokens this service issues, so its own protected
/// endpoints ("/me") can authorize. It mirrors the consumers' configuration:
/// the same issuer and audiences, <c>sub</c> as the name claim and <c>role</c>
/// as the role claim, with inbound claim mapping disabled so the short claim
/// names survive. The verification key comes from the shared
/// <see cref="IJwtKeyProvider"/>, so an ephemeral dev key still validates.
/// </summary>
public static class AuthenticationSetup
{
    public static IServiceCollection AddUserServiceAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudiences = jwt.Audiences,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // Inject the live signing key(s) once the key provider singleton exists.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IJwtKeyProvider>((options, keyProvider) =>
            {
                if (keyProvider.IsAsymmetric)
                    options.TokenValidationParameters.IssuerSigningKeys = keyProvider.PublicKeys;
                else
                    options.TokenValidationParameters.IssuerSigningKey = keyProvider.SigningCredentials.Key;
            });

        services.AddAuthorization();
        return services;
    }
}
