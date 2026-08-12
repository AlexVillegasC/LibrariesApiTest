# authentication Specification

## Purpose

Issues JWT access tokens at login and requires a valid token before listing books in a library.

## Requirements

### Requirement: Login issues a JWT
The system SHALL accept `POST /login` without authentication. When email and password match the configured credentials, the system SHALL return HTTP 200 with a JSON body containing a `token` field. When they do not match, the system SHALL return HTTP 401.

#### Scenario: Valid credentials
- **WHEN** a client sends `POST /login` with email `admin` and password `1234`
- **THEN** the response status is 200 and the body contains a JWT in `token`

#### Scenario: Invalid credentials
- **WHEN** a client sends `POST /login` with any other email and password pair
- **THEN** the response status is 401 and no token is returned

### Requirement: Listing books requires a valid JWT
The system SHALL reject `GET /api/libraries/{libraryId}/books` when the request has no valid bearer token. Other library and book write routes remain reachable without a token, matching current behavior.

#### Scenario: Missing token
- **WHEN** a client sends `GET /api/libraries/{libraryId}/books` without an Authorization bearer token
- **THEN** the response status is 401

#### Scenario: Valid token
- **WHEN** a client sends `GET /api/libraries/{libraryId}/books` with a bearer token issued by `/login`
- **THEN** the request is authorized and listing proceeds according to the books capability