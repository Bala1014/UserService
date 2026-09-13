using Racinglazing.User.Application.Abstractions.Identity;
using Racinglazing.User.Application.Abstractions.Persistence;
using Racinglazing.User.Application.Abstractions.Services;
using Racinglazing.User.Application.Common;
using Racinglazing.User.Application.Contracts;
using Racinglazing.User.Domain.Common;

namespace Racinglazing.User.Application.Features.Users;

/// <summary>
/// User-profile use cases: reading and editing the signed-in user, and the
/// public/batch lookups the other services use to enrich their responses.
/// </summary>
public interface IUserService
{
    /// <summary>The authenticated user's own profile.</summary>
    Task<UserDto> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>Edits the authenticated user's profile.</summary>
    Task<UserDto> UpdateCurrentAsync(UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>Public reference for a single user.</summary>
    Task<UserRefDto> GetPublicAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Batch public references keyed by id. This is what ForumService's
    /// <c>IUserProfileProvider</c> and RaceService call to turn stored author ids
    /// into display names and avatars in one round trip.
    /// </summary>
    Task<IReadOnlyDictionary<string, UserRefDto>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}

public sealed class UserService(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IUserService
{
    public async Task<UserDto> GetCurrentAsync(CancellationToken ct = default)
    {
        var id = currentUser.RequireUserId();
        var user = await users.GetByIdWithRolesAsync(id, ct)
            ?? throw new NotFoundException("USER_NOT_FOUND", "The account could not be found.");
        return ContractMapper.ToDto(user, ContractMapper.RoleNames(user));
    }

    public async Task<UserDto> UpdateCurrentAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var id = currentUser.RequireUserId();
        var user = await users.GetByIdWithRolesAsync(id, ct)
            ?? throw new NotFoundException("USER_NOT_FOUND", "The account could not be found.");

        user.UpdateProfile(request.DisplayName.Trim(), request.AvatarUrl?.Trim(), request.Bio?.Trim(), clock.UtcNow);
        await uow.SaveChangesAsync(ct);
        return ContractMapper.ToDto(user, ContractMapper.RoleNames(user));
    }

    public async Task<UserRefDto> GetPublicAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("USER_NOT_FOUND", "The requested user could not be found.");
        return ContractMapper.ToRefDto(user);
    }

    public async Task<IReadOnlyDictionary<string, UserRefDto>> GetManyAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new Dictionary<string, UserRefDto>();
        var found = await users.GetManyByIdsAsync(ids, ct);
        return found.ToDictionary(u => u.Id.ToString(), ContractMapper.ToRefDto);
    }
}
