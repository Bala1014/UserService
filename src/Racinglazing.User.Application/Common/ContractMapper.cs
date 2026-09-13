using Racinglazing.User.Application.Contracts;
using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Application.Common;

/// <summary>Maps domain entities onto the contract DTOs returned by the API.</summary>
public static class ContractMapper
{
    public static UserDto ToDto(DomainUser user, IReadOnlyCollection<string> roles) => new(
        user.Id.ToString(),
        user.Email,
        user.EmailVerified,
        user.Username,
        user.DisplayName,
        user.AvatarUrl,
        user.Bio,
        roles,
        user.Status.ToString(),
        user.CreatedAt,
        user.LastLoginAt);

    public static UserRefDto ToRefDto(DomainUser user) => new(
        user.Id.ToString(),
        user.Username,
        user.DisplayName,
        user.AvatarUrl);

    /// <summary>Reads role names off a loaded user's assignments.</summary>
    public static IReadOnlyCollection<string> RoleNames(DomainUser user) =>
        user.Roles
            .Where(r => r.Role is not null)
            .Select(r => r.Role.Name)
            .ToArray();
}
