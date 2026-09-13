using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Racinglazing.User.Application.Common;
using Racinglazing.User.Application.Features.Authentication;
using Racinglazing.User.Application.Features.Users;

namespace Racinglazing.User.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}
