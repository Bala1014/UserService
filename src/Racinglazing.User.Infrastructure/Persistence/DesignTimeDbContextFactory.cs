using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Racinglazing.User.Infrastructure.Persistence;

/// <summary>
/// Used by the EF Core CLI (<c>dotnet ef migrations/database</c>) at design
/// time. The connection string here only needs to be parseable to build the
/// model; the running app supplies its own from configuration.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("USER_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=userservice;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new UserDbContext(options);
    }
}
