## Purpose

Encapsulates the Auth slice of the LibraryService API: authenticating a user via `POST /login` and issuing a JWT used to authorize the books endpoints. The system currently validates against a single hardcoded admin account, matching the existing behavior.

## ADDED Requirements

### Requirement: Login issues a token

The system SHALL authenticate a user via `POST /login` with `email` and `password` and, on success, return a signed JWT.

#### Scenario: Valid credentials
- **WHEN** a client sends `POST /login` with the valid admin credentials
- **THEN** the system responds `200 OK` with a JSON body containing a `token` field whose value is a valid JWT

#### Scenario: Invalid credentials
- **WHEN** a client sends `POST /login` with credentials that do not match the admin account
- **THEN** the system responds `401 Unauthorized`

### Requirement: Token authorizes books endpoints

The system SHALL accept the issued token as a Bearer token to authorize the books endpoints.

#### Scenario: Issued token is accepted
- **WHEN** a client sends a books request with `Authorization: Bearer <token>` where `<token>` was issued by `POST /login`
- **THEN** the system processes the request as authorized

#### Scenario: Missing or invalid token is rejected
- **WHEN** a client sends a books request without a Bearer token or with an invalid one
- **THEN** the system responds `401 Unauthorized`
