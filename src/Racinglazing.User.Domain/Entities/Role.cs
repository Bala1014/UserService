namespace Racinglazing.User.Domain.Entities;

/// <summary>
/// A named role that becomes a <c>role</c> claim in the issued token. The set
/// is intentionally open: RaceService recognises "Administrator"/"Editor"/
/// "Entrant", ForumService recognises "Moderator"/"Admin", and new services can
/// introduce their own names without a schema change here. UserService only
/// owns the assignment; each service decides what a role name grants.
/// </summary>
public sealed class Role
{
    private Role() { }

    public Role(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        NormalizedName = name.Trim().ToUpperInvariant();
        Description = description;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string? Description { get; private set; }
}
