using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Racinglazing.User.Api.Common;
using Racinglazing.User.Api.Identity;
using Racinglazing.User.Application;
using Racinglazing.User.Application.Abstractions.Identity;
using Racinglazing.User.Application.Abstractions.Services;
using Racinglazing.User.Infrastructure;
using Racinglazing.User.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Composition root ---
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();

// One object answers "who is calling" and "from where", shared by both abstractions.
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
builder.Services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<CurrentUser>());

builder.Services.AddUserServiceAuthentication(builder.Configuration);

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationActionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Model-binding failures share the { error } envelope, so a client has one error shape.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m)));
        return new BadRequestObjectResult(
            new ErrorEnvelope(new ErrorBody("VALIDATION_ERROR",
                string.IsNullOrWhiteSpace(message) ? "The request is invalid." : message)));
    };
});

var corsAllowedOrigins = GetCorsAllowedOrigins(builder.Configuration);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    // No origins means no browser origins are trusted. This deliberately avoids a
    // wildcard policy, and bearer authentication does not require credentials.
    if (corsAllowedOrigins.Length == 0)
        return;

    policy.WithOrigins(corsAllowedOrigins)
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization");
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// CORS wraps the exception handler so both normal and error responses sent to a
// trusted browser origin include the policy headers. It also short-circuits
// valid OPTIONS preflight requests with a successful response.
app.UseCors();
app.UseExceptionHandler();

app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
app.MapScalarApiReference(options => options
    .WithTitle("RacingVacing UserService")
    .WithOpenApiRoutePattern("/openapi/{documentName}.json"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

await MigrateAndSeedAsync(app);

app.Run();

// Applies pending migrations and seeds the baseline roles on startup when enabled.
static async Task MigrateAndSeedAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:MigrateOnStartup", true)) return;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("Database:SeedOnStartup", true))
        await UserDbSeeder.SeedAsync(db);
}

// CORS_ALLOWED_ORIGINS is intentionally read directly because a single-underscore
// environment variable is not mapped to the Cors:AllowedOrigins configuration path.
// The existing hierarchical configuration key remains supported as well.
static string[] GetCorsAllowedOrigins(IConfiguration configuration)
{
    var environmentValue = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
    var origins = !string.IsNullOrWhiteSpace(environmentValue)
        ? environmentValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        : configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    return origins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}

// Exposed for integration testing.
public partial class Program;
