using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Racinglazing.User.Application.Abstractions.Persistence;
using Racinglazing.User.Application.Abstractions.Security;
using Racinglazing.User.Application.Abstractions.Services;
using Racinglazing.User.Infrastructure.Persistence;
using Racinglazing.User.Infrastructure.Persistence.Repositories;
using Racinglazing.User.Infrastructure.Security;
using Racinglazing.User.Infrastructure.Services;

namespace Racinglazing.User.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("UserDatabase")
            ?? throw new InvalidOperationException("Connection string 'UserDatabase' was not configured.");

        services.AddDbContext<UserDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UserDbContext>());

        // Repositories.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Options.
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));

        // Security.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IJwtKeyProvider, JwtKeyProvider>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // External auth providers (register only when configured, so the resolver
        // reports an unconfigured provider as unavailable).
        var googleClientId = configuration.GetSection(GoogleAuthOptions.SectionName)["ClientId"];
        if (!string.IsNullOrWhiteSpace(googleClientId))
            services.AddScoped<IExternalAuthProvider, GoogleAuthProvider>();
        services.AddScoped<IExternalAuthProviderResolver, ExternalAuthProviderResolver>();

        // Cross-cutting.
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
