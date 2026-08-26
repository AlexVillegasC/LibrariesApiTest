## Why

Auth is a hardcoded `admin`/`1234` check that mints an HMAC JWT. That cannot persist users, roles, session revoke, or subscription state, and it blocks the RBAC work planned as a follow-up change. Replace it with ASP.NET Core Identity in this API so register/login/logout/refresh and admin user administration are real.

## What Changes

- Replace the hardcoded `AuthenticationService` / `TokenGenerator` JWT login with ASP.NET Core Identity (`UserManager` / `SignInManager`) hosted in this API. Duende IdentityServer and `MapIdentityApi` are out of this change.
- **BREAKING**: `POST /login` returns Identity bearer **access** and **refresh** tokens (not a JWT `token` field). Access ~1 hour, refresh ~14 days.
- Add custom endpoints (not `MapIdentityApi`): `POST /register` (`Subscription_L1`), `POST /login`, `POST /logout`, `POST /refresh`, gated `POST /admin/register`, `GET /users/me`, Admin `GET /users` and `GET /users/{id}`, Admin `PATCH` for `IsActive` and `PATCH` for `SubscriptionExpirationDate`.
- Persist Identity on the existing Postgres database by making `LibraryContext` an `IdentityDbContext`. Add `IsActive` and `SubscriptionExpirationDate` on the user. Roles: `Admin`, `Subscription_L1`.
- Database is the source of truth for revocation (SecurityStamp / refresh tokens). Cache is a lookup only.
- **BREAKING** (tests): drop password `1234`; use policy-compliant passwords (length ≥ 6, letter + digit, no symbol required).
- Books stay “any authenticated user.” Libraries stay anonymous. Role-based write/read split is a **separate** follow-up change.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `user-auth`: Replace single hardcoded JWT login with Identity-backed register/login/logout/refresh, roles, `IsActive`, subscription expiry, and Admin user APIs.
- `book-management`: Books continue to require a Bearer token issued by login, but that token is an Identity access token rather than an HMAC JWT. Authorization remains “any authenticated user” (no Admin vs L1 split yet).

## Impact

- **Code**: `LibraryService.Modules.Auth` (replace JWT helper stack), `LibraryContext` → `IdentityDbContext`, `Startup` authentication (Identity bearer instead of JWT bearer), EF migrations.
- **APIs**: `/login` response shape changes; new auth/user routes listed above. `/api/libraries` unchanged (still anonymous). `/api/libraries/{id}/books` still requires Bearer auth without role checks.
- **Tests**: `LibraryService.Specs` UserAuth scenarios rewritten; `AuthClient` login helper updated; book specs keep sending Bearer tokens from the new login.
- **Data**: New Identity tables plus custom user columns on the existing Postgres database.
- **Out of scope**: Duende, cookies, `MapIdentityApi` email/2FA/password-reset routes, update-role, `Subscription_L2`, rate limits, per-session kill list, closing the first-Admin create race, RBAC on libraries/books.
