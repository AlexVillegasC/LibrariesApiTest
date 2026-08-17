## Purpose

Encapsulates the Books slice of the LibraryService API: listing and creating books that belong to a library. Books are always addressed through their owning library and require a valid JWT, mirroring the current authenticated surface.

## Requirements

### Requirement: List books of a library

The system SHALL return all books of a library via `GET /api/libraries/{libraryId}/books` for an authenticated client.

#### Scenario: Library exists and has books
- **WHEN** an authenticated client sends `GET /api/libraries/{libraryId}/books` for an existing library that has books
- **THEN** the system responds `200 OK` with a JSON array of the library's books

#### Scenario: Library exists and has no books
- **WHEN** an authenticated client sends `GET /api/libraries/{libraryId}/books` for an existing library with no books
- **THEN** the system responds `200 OK` with an empty JSON array

#### Scenario: Library does not exist
- **WHEN** an authenticated client sends `GET /api/libraries/{libraryId}/books` for a non-existent library
- **THEN** the system responds `404 Not Found`

#### Scenario: Unauthenticated request
- **WHEN** a client without a valid JWT sends `GET /api/libraries/{libraryId}/books`
- **THEN** the system responds `401 Unauthorized`

### Requirement: Create a book in a library

The system SHALL create a book in an existing library via `POST /api/libraries/{libraryId}/books` for an authenticated client.

#### Scenario: Library exists
- **WHEN** an authenticated client sends `POST /api/libraries/{libraryId}/books` with a valid book payload for an existing library
- **THEN** the system creates the book, associates it with the library, and responds `201 Created` with the created book JSON

#### Scenario: Library does not exist
- **WHEN** an authenticated client sends `POST /api/libraries/{libraryId}/books` for a non-existent library
- **THEN** the system responds `404 Not Found`

#### Scenario: Unauthenticated request
- **WHEN** a client without a valid JWT sends `POST /api/libraries/{libraryId}/books`
- **THEN** the system responds `401 Unauthorized`
