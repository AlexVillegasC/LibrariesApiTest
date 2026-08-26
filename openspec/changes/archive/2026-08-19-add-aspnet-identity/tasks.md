## 1. Identity data model

- [x] 1.1 Add Identity packages (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` and host Identity/bearer bits as needed for net10) to SharedKernel and/or Auth and the API project
- [x] 1.2 Add `ApplicationUser` (`IsActive`, `SubscriptionExpirationDate`) and convert `LibraryContext` to `IdentityDbContext<ApplicationUser>`
- [x] 1.3 Add EF migration for Identity tables + custom user columns; keep `Database.Migrate()` in `Startup`
- [x] 1.4 Seed roles `Admin` and `Subscription_L1` on startup if missing

## 2. Host authentication

- [x] 2.1 Register Identity (`AddIdentity` / `AddIdentityCore` + roles + token providers) with password policy: length ≥ 6, digit, lowercase letter, no symbol required
- [x] 2.2 Replace JWT bearer with Identity bearer (`AddBearerToken`, access ~1 hour, refresh ~14 days); remove `TokenGenerator` / `JwtSettings` request-auth path
- [x] 2.3 Add default authorization policy: authenticated + `IsActive` → 401 if inactive; `Subscription_L1` past expiry → 403; Admin skips expiry. Optional result handler so inactive-with-token is 401
- [x] 2.4 Add `IMemoryCache` as a lookup in front of SecurityStamp / refresh-token checks; database remains source of truth

## 3. Auth endpoints

- [x] 3.1 Implement `POST /register` (anonymous, role `Subscription_L1`, `IsActive=true`, expiry now+1 year UTC)
- [x] 3.2 Implement `POST /login` returning `accessToken` and `refreshToken`; `401` for bad credentials or inactive
- [x] 3.3 Implement `POST /refresh` and `POST /logout` (logout authenticated; revoke all refresh tokens + SecurityStamp for that user)
- [x] 3.4 Implement `POST /admin/register` (anonymous only when Admin count is 0; otherwise Admin-only; create `Admin`)
- [x] 3.5 Implement `GET /users/me` for any authenticated user
- [x] 3.6 Implement Admin `GET /users` and `GET /users/{id}` (`403` for L1)
- [x] 3.7 Implement Admin `PATCH /users/{id}/active` with last-active-Admin and self-deactivate guards; `IsActive=false` revokes all sessions
- [x] 3.8 Implement Admin `PATCH /users/{id}/subscription`; past date for L1 revokes sessions; do not add an update-role route
- [x] 3.9 Delete hardcoded `AuthenticationService` admin/`1234` login

## 4. Books stay authenticated, libraries unchanged

- [x] 4.1 Point books `RequireAuthorization()` at the new default policy (no `RequireRole` on books or libraries)
- [x] 4.2 Leave `/api/libraries` anonymous (no auth metadata)

## 5. Specs

- [x] 5.1 Update `ApiFactory` so the SQLite test database includes Identity schema on the same connection
- [x] 5.2 Rewrite `AuthClient` to register/login the first Admin with a policy-compliant password and send `accessToken` as Bearer
- [x] 5.3 Replace `UserAuth` specs to cover every `user-auth` delta scenario (register, password policy, login, logout, refresh, admin-register gate, me, directory, IsActive, expiry, no update-role)
- [x] 5.4 Update book specs for Identity access token, inactive `401`, expired L1 `403` on GET books; keep L1 allowed to POST books in this change
- [x] 5.5 Leave library-management specs anonymous (no auth assertions)

## 6. Verification

- [x] 6.1 `dotnet build` the solution succeeds
- [x] 6.2 `dotnet test` on `LibraryService.Specs` is green
