namespace Racinglazing.User.Domain.Entities;

/// <summary>Join row linking a <see cref="User"/> to a <see cref="Role"/>.</summary>
public sealed class UserRole
{
    private UserRole() { }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;
}
