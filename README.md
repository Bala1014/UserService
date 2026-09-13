# RacingVacing — UserService

The identity provider for the RacingVacing platform. It owns accounts,
credentials and roles, and issues the JWTs that **RaceService** and
**ForumService** verify. Those services never see a password — they trust the
tokens minted here. This is the authentication boundary for the whole platform.

## What it does today

- **Register** with email, username, password + confirmation.
- **Login** with email *or* username + password.
- **Refresh-token rotation** with reuse (replay) detection.
- **Logout** (refresh-token revocation).
- **Google sign-in** (OIDC `id_token`) — extensible to other providers.
- **Profile** read/edit for the signed-in user (`/me`).
- **Public + batch user lookup** for the other services' display enrichment.
- **OIDC discovery + JWKS** so consumers can verify signatures without a shared
  secret.

## Architecture

Clean architecture, matching ForumService's layout:

```
Racinglazing.User.Domain          Entities, value rules, domain exceptions. No dependencies.
Racinglazing.User.Application     Use cases (AuthService, UserService), contracts, abstractions.
Racinglazing.User.Infrastructure  EF Core + Postgres, password hashing, JWT issuance, Google.
Racinglazing.User.Api             Controllers, auth wiring, composition root.
```

Dependencies point inward (Api → Application → Domain; Infrastructure
implements Application's abstractions). Everything crossing a boundary — the
password hasher, the token service, each external-auth provider, the clock — is
an interface, so the signing scheme or hashing algorithm can change without
touching a use case.

## Running locally

```bash
docker compose up --build
```

UserService is on `http://localhost:5002`, Postgres on `5432`. Migrations apply
and the baseline roles seed on startup.

To run the API on the host against the compose Postgres:

```bash
dotnet run --project src/Racinglazing.User.Api
```

Interactive API docs (Scalar): `http://localhost:5002/scalar/v1`.
See [`requests.http`](requests.http) for ready-to-run calls.

## Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | `/api/v1/auth/register` | — | Create an account, get a token pair |
| POST | `/api/v1/auth/login` | — | Email/username + password |
| POST | `/api/v1/auth/refresh` | — | Rotate the token pair |
| POST | `/api/v1/auth/logout` | — | Revoke a refresh token |
| POST | `/api/v1/auth/google` | — | Sign in / provision via Google `id_token` |
| GET | `/api/v1/users/me` | Bearer | Current user's profile |
| PUT | `/api/v1/users/me` | Bearer | Edit profile |
| GET | `/api/v1/users/{id}` | — | Public user reference |
| POST | `/api/v1/users/batch` | — | Batch public references (for Forum/Race) |
| GET | `/.well-known/openid-configuration` | — | OIDC discovery |
| GET | `/.well-known/jwks.json` | — | Public signing keys (RS256) |
| GET | `/health/live`, `/health/ready` | — | Probes |

Responses use the shared envelope: `{ "data": ... }` on success,
`{ "error": { "code", "message" } }` on failure.

## The token contract (how the services integrate)

Access tokens are JWTs with the claims the consumers already read:

| Claim | Value |
|-------|-------|
| `iss` | `racingvacing-userservice` |
| `aud` | `["racingvacing-forumservice", "racingvacing-raceservice", "racingvacing-userservice"]` |
| `sub` | user id (GUID) |
| `role` | one claim per role (`User`, `Administrator`, `Moderator`, `Editor`, `Entrant`, …) |
| `email`, `email_verified`, `preferred_username`, `name` | profile claims |
| `security_stamp` | rotated on credential change, for session invalidation |

RaceService and ForumService already expect exactly this issuer, these
audiences and these claim names (`sub` as the name claim, `role` as the role
claim, inbound mapping off).

### Signing: two modes

- **RS256 (default, production):** tokens are signed with an RSA private key and
  verified against the public key published at `/.well-known/jwks.json`. No
  shared secret; keys rotate automatically. Consumers set `Jwt:Authority` to
  this service's URL.
- **HS256 (local dev):** a shared `Jwt:SigningKey`. Simpler for running one
  service at a time; no discovery needed. This is the compose/default-dev mode.

### Wiring the consumers

**RaceService / ForumService** — point them at this service:

```jsonc
// RS256 (production)
"Jwt": {
  "Authority": "https://userservice",        // RaceService fetches JWKS from here
  "Issuer":   "racingvacing-userservice",
  "Audience": "racingvacing-raceservice"      // or racingvacing-forumservice
}
// HS256 (local dev) — instead of Authority:
"Jwt": { "SigningKey": "<same key as UserService Jwt:SigningKey>", "Issuer": "...", "Audience": "..." }
```

ForumService additionally resolves author display data through its
`IUserProfileProvider` (currently a stub). Swap the stub for an HTTP client that
calls `POST /api/v1/users/batch` here — the returned
`{ id: { id, username, displayName, avatarUrl } }` map is already the shape its
`UserRefDto` expects.

## Configuration

| Key | Meaning |
|-----|---------|
| `ConnectionStrings:UserDatabase` | Postgres connection string |
| `Database:MigrateOnStartup` / `SeedOnStartup` | apply migrations / seed roles on boot |
| `Jwt:Algorithm` | `RS256` or `HS256` |
| `Jwt:PrivateKeyPem` | RSA private key (PEM) for RS256 in production |
| `Jwt:SigningKey` | shared secret for HS256 |
| `Jwt:Issuer` / `Jwt:Audiences` | token `iss` / `aud` |
| `Jwt:AccessTokenLifetimeMinutes` | access-token TTL (default 15) |
| `Auth:RefreshTokenLifetimeDays` | refresh-token TTL (default 14) |
| `Auth:ExternalAutoProvision` | create an account on first Google sign-in |
| `Google:ClientId` | Google OAuth client id (enables Google sign-in) |
| `Cors:AllowedOrigins` | allowed browser origins |

## Security notes

- Passwords are hashed with PBKDF2-HMAC-SHA256 (210k iterations), never stored
  or logged in plaintext. Hashes are self-describing and upgraded transparently
  when the work factor rises.
- Refresh tokens are stored only as SHA-256 hashes; the raw value is shown once.
  Rotation + replay detection revokes a whole session family if a used token
  reappears.
- Login errors are deliberately identical for "unknown user" and "wrong
  password" to prevent account enumeration.
- Production must supply a persistent `Jwt:PrivateKeyPem`; the ephemeral dev key
  is logged with a warning and does not survive a restart.

## Database

Five tables (snake_case, via `EFCore.NamingConventions`): `users`, `roles`,
`user_roles`, `external_logins`, `refresh_tokens`. Uniqueness is enforced on
normalised (case-insensitive) email and username. Migrations live in
`src/Racinglazing.User.Infrastructure/Persistence/Migrations`.

```bash
# add a migration
dotnet ef migrations add <Name> \
  --project src/Racinglazing.User.Infrastructure \
  --startup-project src/Racinglazing.User.Api \
  --output-dir Persistence/Migrations
```
