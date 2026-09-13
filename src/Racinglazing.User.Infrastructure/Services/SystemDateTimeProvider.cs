using Racinglazing.User.Application.Abstractions.Services;

namespace Racinglazing.User.Infrastructure.Services;

/// <summary>The real system clock, in UTC.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
