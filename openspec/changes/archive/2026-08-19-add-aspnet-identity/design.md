## Context

See `proposal.md` for motivation. Current constraints:

- `POST /login` uses a hardcoded `admin`/`1234` check and `TokenGenerator` HMAC JWT (`ClaimTypes.Role` = `"admin"`). `Startup` uses `AddJwtBearer` with `JwtSettings`.
- Books endpoints `.RequireAuthorization()` with no roles. Libraries endpoints have no auth.
- `LibraryContext` is a pooled Npgsql `DbContext` with `Library` and `Book` only. Specs replace it with a singleton SQLite `:memory:` context.
- Auth lives in `LibraryService.Modules.Auth` as `AuthController` + helpers. Host is still `Startup` + `MapControllers` + `MapLibraries` / `MapBooks`.
- Frontend CORS origin `http://localhost:5173` is already configured; this change uses bearer tokens (no credentialed cookies).

## Goals / Non-Goals

**Goals:**
- Identity as the user/role store in this API, custom register/login/logout/refresh, Admin user APIs, bearer tokens, `IsActive`/expiry enforcement on authenticated routes.
- Same Postgres database; Identity schema via EF migrations on `LibraryContext`.
- Specs and `AuthClient` use a policy-compliant password instead of `1234`.

**Non-Goals:**
- Duende IdentityServer, cookie auth, `MapIdentityApi`, email/2FA/password reset.
- Update-role, `Subscription_L2`, rate limits, per-session kill list, closing the first-Admin create race.
- RBAC on libraries/books (L1 read vs Admin write) and requiring auth on libraries — follow-up change.
- Changing library HTTP contracts.

## Decisions

### D1: ASP.NET Core Identity in-process, not Duende, not MapIdentityApi

Use `UserManager` / `SignInManager` / `RoleManager` with custom endpoints. Do not call `MapIdentityApi` (it would publish confirm-email, forgot-password, 2FA). Do not add Duende.

- **Rationale**: Locked in grilling; this change only needs register/login/logout/refresh plus Admin user APIs.
- **Alternative**: `MapIdentityApi` with unused email routes — rejected (attack surface). Duende — rejected (OIDC product, not the user store).

### D2: Identity bearer tokens, not JWT, not cookies

Replace `AddJwtBearer` with Identity bearer (`AddBearerToken` / `IdentityConstants.BearerScheme`). Login returns `accessToken` + `refreshToken` (Identity `AccessTokenResponse` shape). Access ~1 hour, refresh ~14 days.

- **Rationale**: Cross-origin Vite FE cannot rely on first-party cookies without SameSite=None/HTTPS. Opaque Identity tokens are the Identity API standard; they are not JWTs.
- **Alternative**: Keep HMAC JWT — rejected (logout/disable would be a lie without extra denylist work we already folded into Identity refresh + stamp). Cookies — rejected for split FE/API.

### D3: LibraryContext becomes IdentityDbContext

`LibraryContext : IdentityDbContext<ApplicationUser>` on the existing connection string. `ApplicationUser` adds `IsActive` (bool, default true) and `SubscriptionExpirationDate` (`DateTimeOffset`). Seed roles `Admin` and `Subscription_L1`.

Specs SQLite factory must still use one shared connection so Identity and library tables coexist in the in-memory database.

- **Rationale**: One database, one migration pipeline, as locked.
- **Alternative**: Second DbContext on the same connection — rejected (two migrators, two test wiring paths).

### D4: Revocation source of truth is the database

Logout, `IsActive=false`, and L1 expiry-in-the-past increment SecurityStamp and delete that user's refresh tokens. `IMemoryCache` (or `IDistributedCache` if already present) may cache stamp/deny lookups; a miss hits the database. Flushing cache must not un-revoke.

- **Rationale**: In-memory denylist-only is fail-open on restart.
- **Alternative**: Cache as sole denylist — rejected.

### D5: Authorization split — policies, not a route-map middleware

```
UseAuthentication()
UseAuthorization()
```

- Default policy for authenticated endpoints: valid bearer + `IsActive` (fail → **401**) + if role is `Subscription_L1` and `SubscriptionExpirationDate` is in the past → **403**. Skip expiry for Admin.
- Books group keeps `RequireAuthorization()` (default policy). No `RequireRole` on books or libraries in this change.
- Admin user routes: `RequireRole("Admin")`.
- `POST /register`, `POST /login`, `POST /refresh`: anonymous. `POST /admin/register`: anonymous only when Admin count is 0; otherwise `RequireRole("Admin")` (evaluate at request time).
- Optional thin `IAuthorizationMiddlewareResultHandler` (or equivalent) so inactive-with-valid-token maps to 401 rather than 403. Do **not** encode the library/book route table in middleware.

Libraries stay without `RequireAuthorization`.

- **Rationale**: RBAC per verb belongs on endpoint groups in the follow-up; active/expiry are global authenticated rules.
- **Alternative**: Middleware that switches on path/verb — rejected.

### D6: Endpoint list and shapes

| Method | Path | Auth |
|---|---|---|
| POST | `/register` | anonymous → L1 |
| POST | `/login` | anonymous |
| POST | `/logout` | bearer |
| POST | `/refresh` | anonymous body (`refreshToken`) |
| POST | `/admin/register` | gated |
| GET | `/users/me` | bearer |
| GET | `/users` | Admin |
| GET | `/users/{id}` | Admin |
| PATCH | `/users/{id}/active` | Admin (`{ "isActive": bool }`) |
| PATCH | `/users/{id}/subscription` | Admin (`{ "subscriptionExpirationDate": "<ISO-8601>" }`) |

No `PATCH /users/{id}/role`. Keep `AuthController` or equivalent MapGroup in the Auth module; books/libraries stay Minimal APIs.

Password policy in Identity options: `RequiredLength = 6`, `RequireDigit = true`, `RequireNonAlphanumeric = false`, require a letter via `RequireUppercase` **or** `RequireLowercase` set so that `abcde1` succeeds (set `RequireUppercase = false`, `RequireLowercase = true`, `RequireDigit = true`).

### D7: Last-Admin and self-deactivate guards

In the IsActive handler: reject if target is the caller; reject if target is Admin and would leave zero Admins with `IsActive=true`. Role changes do not exist, so demotion is impossible.

### D8: Specs bootstrap

Replace `AuthClient.LoginAsAdminAsync` hardcoded `admin`/`1234` with: ensure first Admin via `/admin/register` (or login if already present) using a policy-compliant password stored in test helpers (e.g. `Admin1a`). L1 tests use `/register`. Token header uses `accessToken` from login JSON.

## Risks / Trade-offs

- **[Risk] First Admin race** → Accepted; two anonymous `/admin/register` calls may both succeed. No serializing transaction in this change.
- **[Risk] Libraries remain anonymous** → Inactive/expired users (and the public) can still mutate libraries until the follow-up RBAC change.
- **[Risk] L1 can still POST books** → Accepted for this change; follow-up makes writes Admin-only.
- **[Risk] Opaque bearer tokens** → Existing JWT-decoding clients break. Specs and FE must use `accessToken` + refresh.
- **[Risk] No rate limit on register/login** → Accepted for this test API.
- **[Risk] Identity on SQLite in specs** → Use the same singleton connection as today so Identity tables are visible; add Identity migrations or `EnsureCreated` consistent with current spec DB setup.

## Migration Plan

1. Add Identity packages, `ApplicationUser`, convert `LibraryContext`, generate EF migration, keep `Database.Migrate()` in `Startup`.
2. Wire Identity + bearer in `Startup`; remove JWT settings usage for request auth.
3. Replace `AuthController` endpoints; add Admin user routes.
4. Point books `RequireAuthorization` at the new default policy (active + L1 expiry).
5. Update `LibraryService.Specs` and `AuthClient`.
6. Rollback: revert migration and auth module; no data migration off Identity is provided.

## Open Questions

None that affect specs or this approach. JSON property casing follows existing API JSON options. Exact Identity package versions follow the `net10.0` host.
