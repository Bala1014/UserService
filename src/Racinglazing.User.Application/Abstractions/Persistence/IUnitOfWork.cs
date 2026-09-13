namespace Racinglazing.User.Application.Abstractions.Persistence;

/// <summary>
/// Commits all work staged across repositories as one atomic transaction.
/// Repositories stage; the use-case decides when to commit.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
