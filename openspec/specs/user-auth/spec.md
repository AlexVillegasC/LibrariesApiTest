## Purpose

Encapsulates the Auth slice of the LibraryService API: authenticating a user via `POST /login` and issuing a JWT used to authorize the books endpoints. The system currently validates against a single hardcoded admin account, matching the existing behavior.

## Requirements

### Requirement: Login issues a token

The system SHALL authenticate a user via `POST /login` with `email` and `password`. On success it SHALL return Identity bearer `accessToken` and `refreshToken` (not a JWT `token` field). Access tokens SHALL expire in approximately one hour. Refresh tokens SHALL expire in approximately 14 days. Login SHALL fail with `401 Unauthorized` for unknown credentials or when `IsActive` is false.

#### Scenario: Valid credentials
- **WHEN** a client sends `POST /login` with the email and password of an active registered user
- **THEN** the system responds `200 OK` with JSON containing `accessToken` and `refreshToken`

#### Scenario: Invalid credentials
- **WHEN** a client sends `POST /login` with credentials that do not match an active user
- **THEN** the system responds `401 Unauthorized`

#### Scenario: Inactive user cannot login
- **WHEN** a client sends `POST /login` with credentials of a user whose `IsActive` is false
- **THEN** the system responds `401 Unauthorized`

### Requirement: Token authorizes books endpoints

The system SHALL accept the login `accessToken` as `Authorization: Bearer` to authorize the books endpoints for any authenticated active user (Admin or Subscription_L1). Role-based write restrictions on books are out of this change.

#### Scenario: Issued token is accepted
- **WHEN** a client sends a books request with `Authorization: Bearer <accessToken>` where `<accessToken>` was issued by `POST /login` for an active user
- **THEN** the system processes the request as authorized (unless that user is an expired Subscription_L1, which is `403 Forbidden`)

#### Scenario: Missing or invalid token is rejected
- **WHEN** a client sends a books request without a Bearer token or with an invalid one
- **THEN** the system responds `401 Unauthorized`

#### Scenario: Inactive token is rejected
- **WHEN** a client sends a books request with a Bearer access token for a user whose `IsActive` is false
- **THEN** the system responds `401 Unauthorized`

### Requirement: Register Subscription_L1

The system SHALL create a `Subscription_L1` user via anonymous `POST /register` with `email` and `password`. A new L1 user SHALL be `IsActive=true` and SHALL have `SubscriptionExpirationDate` set to one year from registration (UTC).

#### Scenario: Successful L1 register
- **WHEN** a client sends `POST /register` with a unique email and a policy-compliant password
- **THEN** the system responds `200 OK` and the created user has role `Subscription_L1`, `IsActive` true, and `SubscriptionExpirationDate` approximately one year in the future

#### Scenario: Duplicate email
- **WHEN** a client sends `POST /register` with an email that already exists
- **THEN** the system responds `400 Bad Request`

#### Scenario: Password below policy
- **WHEN** a client sends `POST /register` with a password shorter than 6 characters, or with no letter, or with no digit
- **THEN** the system responds `400 Bad Request`

### Requirement: Password policy

The system SHALL require passwords of length at least 6 that contain at least one letter and at least one digit. The system SHALL NOT require a symbol.

#### Scenario: Letter and digit of length 6 is accepted
- **WHEN** a client registers with password `abcde1`
- **THEN** the system does not reject the password for policy reasons

### Requirement: Admin register

The system SHALL create an `Admin` user via `POST /admin/register` with `email` and `password`. That endpoint SHALL allow an anonymous caller only when no `Admin` user exists. After at least one `Admin` exists, only an authenticated `Admin` MAY call it. A concurrent double-create while zero Admins exist is an accepted risk and is not specified.

#### Scenario: First Admin when none exist
- **WHEN** no Admin exists and an anonymous client sends `POST /admin/register` with a unique email and a policy-compliant password
- **THEN** the system responds `200 OK` and the created user has role `Admin`

#### Scenario: Anonymous admin-register after an Admin exists
- **WHEN** at least one Admin exists and an anonymous client sends `POST /admin/register`
- **THEN** the system responds `401 Unauthorized`

#### Scenario: Admin creates another Admin
- **WHEN** an authenticated Admin sends `POST /admin/register` with a unique email and a policy-compliant password
- **THEN** the system responds `200 OK` and the created user has role `Admin`

#### Scenario: L1 cannot admin-register
- **WHEN** an authenticated Subscription_L1 user sends `POST /admin/register`
- **THEN** the system responds `403 Forbidden`

### Requirement: Logout revokes all sessions

The system SHALL revoke all sessions for the authenticated user via `POST /logout` (Authorization Bearer access token). After logout, existing refresh tokens for that user SHALL NOT mint new access tokens.

#### Scenario: Logout then refresh fails
- **WHEN** an authenticated user sends `POST /logout` and then `POST /refresh` with a refresh token that previously worked for that user
- **THEN** logout responds success and refresh responds `401 Unauthorized`

### Requirement: Refresh issues new tokens

The system SHALL exchange a valid refresh token via `POST /refresh` for a new access token and refresh token.

#### Scenario: Valid refresh
- **WHEN** a client sends `POST /refresh` with a still-valid refresh token
- **THEN** the system responds `200 OK` with a new access token and refresh token

#### Scenario: Refresh after logout
- **WHEN** a client sends `POST /refresh` with a refresh token for a user who has logged out
- **THEN** the system responds `401 Unauthorized`

### Requirement: Current user profile

The system SHALL return the authenticated user's `email`, role, `IsActive`, and `SubscriptionExpirationDate` via `GET /users/me`.

#### Scenario: L1 reads self
- **WHEN** an authenticated Subscription_L1 user sends `GET /users/me`
- **THEN** the system responds `200 OK` with that user's role `Subscription_L1`, `IsActive`, and `SubscriptionExpirationDate`

#### Scenario: Unauthenticated me
- **WHEN** a client without a valid access token sends `GET /users/me`
- **THEN** the system responds `401 Unauthorized`

### Requirement: Admin user directory

The system SHALL list users via `GET /users` and return one user via `GET /users/{id}` for an authenticated Admin only.

#### Scenario: Admin lists users
- **WHEN** an authenticated Admin sends `GET /users`
- **THEN** the system responds `200 OK` with a JSON array of users

#### Scenario: Admin gets user by id
- **WHEN** an authenticated Admin sends `GET /users/{id}` for an existing user
- **THEN** the system responds `200 OK` with that user

#### Scenario: L1 cannot list users
- **WHEN** an authenticated Subscription_L1 user sends `GET /users`
- **THEN** the system responds `403 Forbidden`

#### Scenario: L1 cannot get user by id
- **WHEN** an authenticated Subscription_L1 user sends `GET /users/{id}`
- **THEN** the system responds `403 Forbidden`

### Requirement: Admin sets IsActive

The system SHALL update a user's `IsActive` via an Admin-only route (separate from the expiry route). Setting `IsActive` to false SHALL revoke all of that user's sessions. Every subsequent request with a previously valid access token for that user SHALL return `401 Unauthorized`. Login with those credentials SHALL return `401 Unauthorized`. Disable and `IsActive` are the same field; there is no second disable flag.

An Admin SHALL NOT set `IsActive=false` on their own account. The system SHALL reject setting `IsActive=false` on the last remaining active Admin.

#### Scenario: Admin deactivates L1
- **WHEN** an Admin sets `IsActive` false on a Subscription_L1 user
- **THEN** subsequent API calls using that user's access token return `401 Unauthorized` and login with that user returns `401 Unauthorized`

#### Scenario: Last active Admin cannot be deactivated
- **WHEN** an Admin attempts to set `IsActive` false on the last remaining active Admin
- **THEN** the system rejects the request and that Admin remains active

#### Scenario: Admin cannot deactivate self
- **WHEN** an Admin attempts to set `IsActive` false on their own account
- **THEN** the system rejects the request and that Admin remains active

#### Scenario: L1 cannot set IsActive
- **WHEN** an authenticated Subscription_L1 user calls the IsActive Admin route
- **THEN** the system responds `403 Forbidden`

### Requirement: Admin sets subscription expiration

The system SHALL update `SubscriptionExpirationDate` via an Admin-only route separate from the IsActive route. When that date is in the past, every request by that user SHALL return `403 Forbidden` if the user is `Subscription_L1`. The expiry check SHALL NOT apply to Admin. Setting expiry to the past for L1 SHALL revoke all of that user's sessions.

#### Scenario: Expired L1 is forbidden
- **WHEN** an Admin sets a Subscription_L1 user's `SubscriptionExpirationDate` to a past UTC time and that user then calls an authenticated endpoint with a still-valid access token
- **THEN** the system responds `403 Forbidden`

#### Scenario: Admin is not gated by expiry
- **WHEN** an Admin has a past `SubscriptionExpirationDate` and calls an authenticated endpoint
- **THEN** the system does not reject the call because of expiry

#### Scenario: L1 cannot set expiry
- **WHEN** an authenticated Subscription_L1 user calls the subscription-expiration Admin route
- **THEN** the system responds `403 Forbidden`

### Requirement: No update-role route

The system SHALL NOT expose an endpoint that changes a user's role. New Admins are created only via `POST /admin/register`.

#### Scenario: Update-role is absent
- **WHEN** a client sends a request to an update-role path such as `PATCH /users/{id}/role`
- **THEN** the system responds `404 Not Found`
