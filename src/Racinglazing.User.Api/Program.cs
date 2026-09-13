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

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
app.MapScalarApiReference(options => options
    .WithTitle("RacingVacing UserService")
    .WithOpenApiRoutePattern("/openapi/{documentName}.json"));

app.UseCors();
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

// Exposed for integration testing.
public partial class Program;
