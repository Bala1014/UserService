using Microsoft.Extensions.Options;
using Racinglazing.User.Application.Abstractions.Persistence;
using Racinglazing.User.Application.Abstractions.Security;
using Racinglazing.User.Application.Abstractions.Services;
using Racinglazing.User.Application.Common;
using Racinglazing.User.Application.Contracts;
using Racinglazing.User.Domain.Common;
using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Application.Features.Authentication;

/// <summary>
/// The authentication use cases: register, login, token refresh, logout and
/// external (Google) sign-in. This is the only place that turns a credential
/// into a session — the other services merely verify the tokens it issues.
/// </summary>
public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResultDto> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
    Task<AuthResultDto> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default);
}

public sealed class AuthService(
    IUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IRefreshTokenGenerator refreshTokenGenerator,
    IExternalAuthProviderResolver externalProviders,
    IDateTimeProvider clock,
    IRequestContext requestContext,
    IOptions<AuthOptions> options) : IAuthService
{
    private readonly AuthOptions _options = options.Value;

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var username = request.Username.Trim();

        // Check both up front so the caller gets the most relevant conflict, rather
        // than whichever unique index the database happened to hit first.
        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("EMAIL_TAKEN", "An account with this email already exists.");
        if (await users.UsernameExistsAsync(username, ct))
            throw new ConflictException("USERNAME_TAKEN", "This username is already taken.");

        var user = DomainUser.Register(email, username, username, passwordHasher.Hash(request.Password), clock.UtcNow);
        await AssignDefaultRoleAsync(user, ct);
        users.Add(user);

        var result = await IssueAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailOrUsernameAsync(request.EmailOrUsername.Trim(), ct);

        // A missing user and a wrong password return the same error, so the endpoint
        // can't be used to enumerate which emails are registered.
        if (user is null || !user.HasPassword)
            throw new UnauthorizedException("INVALID_CREDENTIALS", "The email/username or password is incorrect.");

        var verification = passwordHasher.Verify(user.PasswordHash!, request.Password);
        if (!verification.Succeeded)
            throw new UnauthorizedException("INVALID_CREDENTIALS", "The email/username or password is incorrect.");

        if (!user.CanAuthenticate)
            throw new ForbiddenException("ACCOUNT_DISABLED", "This account is not permitted to sign in.");

        if (verification.NeedsRehash)
            user.UpgradePasswordHash(passwordHasher.Hash(request.Password), clock.UtcNow);

        user.RecordLogin(clock.UtcNow);
        var result = await IssueAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task<AuthResultDto> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var presentedHash = refreshTokenGenerator.Hash(request.RefreshToken);
        var token = await refreshTokens.GetByHashAsync(presentedHash, ct)
            ?? throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "The refresh token is invalid.");

        var now = clock.UtcNow;

        // A token presented after it was already rotated is a replay — treat the
        // whole session family as compromised and revoke every active token.
        if (token.IsRevoked)
        {
            await RevokeAllActiveAsync(token.UserId, now, ct);
            await uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("REFRESH_TOKEN_REUSED", "The refresh token has already been used.");
        }

        if (!token.IsActive(now))
            throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "The refresh token is invalid.");

        var user = token.User
            ?? throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "The refresh token is invalid.");

        if (!user.CanAuthenticate)
            throw new ForbiddenException("ACCOUNT_DISABLED", "This account is not permitted to sign in.");

        // Rotate: mint the successor, point the old token at it, then issue the pair.
        var rolesList = ContractMapper.RoleNames(user);
        var access = tokenService.CreateAccessToken(user, rolesList);
        var (rawRefresh, refreshHash) = refreshTokenGenerator.Create();
        var expiresAt = now.AddDays(_options.RefreshTokenLifetimeDays);

        token.Revoke(now, requestContext.IpAddress, refreshHash);
        user.IssueRefreshToken(refreshHash, expiresAt, now, requestContext.IpAddress);

        await uow.SaveChangesAsync(ct);
        return new AuthResultDto(
            access.Value, "Bearer", access.ExpiresInSeconds,
            rawRefresh, expiresAt, ContractMapper.ToDto(user, rolesList));
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var token = await refreshTokens.GetByHashAsync(refreshTokenGenerator.Hash(request.RefreshToken), ct);

        // Logout is idempotent: an unknown or already-revoked token is a no-op,
        // not an error, so a client can always "sign out" safely.
        if (token is null || token.IsRevoked) return;

        token.Revoke(clock.UtcNow, requestContext.IpAddress);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<AuthResultDto> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var provider = externalProviders.Resolve("google")
            ?? throw new ValidationFailedException("PROVIDER_UNAVAILABLE", "Google sign-in is not configured.");

        var info = await provider.ValidateAsync(request.IdToken, ct)
            ?? throw new UnauthorizedException("INVALID_EXTERNAL_TOKEN", "The Google token could not be verified.");

        // 1) Already linked — straight in.
        var user = await users.GetByExternalLoginAsync(info.Provider, info.Subject, ct);

        // 2) Known email — link Google to the existing account.
        if (user is null)
        {
            user = await users.GetByEmailAsync(info.Email, ct);
            if (user is not null)
                user.LinkExternalLogin(info.Provider, info.Subject, clock.UtcNow);
        }

        // 3) New user — provision one if policy allows.
        if (user is null)
        {
            if (!_options.ExternalAutoProvision)
                throw new ForbiddenException("NO_ACCOUNT", "No account exists for this Google identity.");

            var username = await GenerateUniqueUsernameAsync(info.Email, ct);
            var displayName = string.IsNullOrWhiteSpace(info.Name) ? username : info.Name!;
            user = DomainUser.RegisterExternal(
                info.Email, username, displayName, info.EmailVerified, info.Provider, info.Subject, clock.UtcNow);
            if (!string.IsNullOrWhiteSpace(info.PictureUrl))
                user.UpdateProfile(displayName, info.PictureUrl, null, clock.UtcNow);
            await AssignDefaultRoleAsync(user, ct);
            users.Add(user);
        }

        if (!user.CanAuthenticate)
            throw new ForbiddenException("ACCOUNT_DISABLED", "This account is not permitted to sign in.");

        if (info.EmailVerified) user.MarkEmailVerified(clock.UtcNow);
        user.RecordLogin(clock.UtcNow);

        var result = await IssueAsync(user, ct);
        await uow.SaveChangesAsync(ct);
        return result;
    }

    // Builds a token pair for a user and stages the refresh token. The caller commits.
    private Task<AuthResultDto> IssueAsync(DomainUser user, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var rolesList = ContractMapper.RoleNames(user);
        var access = tokenService.CreateAccessToken(user, rolesList);
        var (rawRefresh, refreshHash) = refreshTokenGenerator.Create();
        var expiresAt = now.AddDays(_options.RefreshTokenLifetimeDays);

        user.IssueRefreshToken(refreshHash, expiresAt, now, requestContext.IpAddress);

        return Task.FromResult(new AuthResultDto(
            access.Value, "Bearer", access.ExpiresInSeconds,
            rawRefresh, expiresAt, ContractMapper.ToDto(user, rolesList)));
    }

    private async Task AssignDefaultRoleAsync(DomainUser user, CancellationToken ct)
    {
        var role = await roles.GetByNameAsync(DefaultRoles.User, ct)
            ?? throw new InvalidOperationException(
                $"The default role '{DefaultRoles.User}' is not seeded. Run database seeding.");
        user.AssignRole(role);
    }

    private async Task RevokeAllActiveAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var active = await refreshTokens.GetActiveForUserAsync(userId, now, ct);
        foreach (var t in active)
            t.Revoke(now, requestContext.IpAddress);
    }

    // Derives a candidate username from the email local-part and suffixes it until unique.
    private async Task<string> GenerateUniqueUsernameAsync(string email, CancellationToken ct)
    {
        var baseName = new string(email.Split('@')[0]
            .Where(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.')
            .ToArray());
        if (baseName.Length < 3) baseName = $"user{baseName}";
        baseName = baseName[..Math.Min(baseName.Length, 24)];

        var candidate = baseName;
        var suffix = 0;
        while (await users.UsernameExistsAsync(candidate, ct))
            candidate = $"{baseName}{++suffix}";
        return candidate;
    }
}
