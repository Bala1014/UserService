using System.Security.Claims;
using Racinglazing.User.Application.Abstractions.Identity;
using Racinglazing.User.Application.Abstractions.Services;
using Racinglazing.User.Domain.Common;

namespace Racinglazing.User.Api.Identity;

/// <summary>
/// Resolves the caller from the validated access token on the current request.
/// UserService consumes its own tokens for the authenticated "/me" endpoints,
/// so identity is read from the same <c>sub</c>/<c>role</c> claims it issues.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser, IRequestContext
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue("sub")
                        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll("role").Select(c => c.Value)
            .Concat(Principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct().ToArray() ?? [];

    public Guid RequireUserId() => UserId
        ?? throw new UnauthorizedException("UNAUTHENTICATED", "Authentication is required for this operation.");

    // IRequestContext — annotate issued/revoked refresh tokens with the caller's IP.
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
