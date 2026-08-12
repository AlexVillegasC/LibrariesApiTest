# books Specification

## Purpose

Exposes nested book operations under a library so clients can list and add books at `/api/libraries/{libraryId}/books`.

## Requirements

### Requirement: List books in a library
The system SHALL return all books for an existing library as JSON with HTTP 200 when a client requests `GET /api/libraries/{libraryId}/books`. The system SHALL return HTTP 404 when the library does not exist.

#### Scenario: Library has books
- **WHEN** an authenticated client sends `GET /api/libraries/{libraryId}/books` for a library that has books
- **THEN** the response status is 200 and the body is a JSON array of those books

#### Scenario: Library has no books
- **WHEN** an authenticated client sends `GET /api/libraries/{libraryId}/books` for an existing library with no books
- **THEN** the response status is 200 and the body is an empty JSON array

#### Scenario: Library missing
- **WHEN** an authenticated client sends `GET /api/libraries/{libraryId}/books` for an id that does not exist
- **THEN** the response status is 404

### Requirement: Add book to a library
The system SHALL persist a new book for `POST /api/libraries/{libraryId}/books` when the library exists, returning HTTP 201. The system SHALL return HTTP 404 when the library does not exist.

#### Scenario: Successful add
- **WHEN** a client sends `POST /api/libraries/{libraryId}/books` with a JSON body containing at least a name for an existing library
- **THEN** the book is stored under that library and the response status is 201

#### Scenario: Add to missing library
- **WHEN** a client sends `POST /api/libraries/{libraryId}/books` for an id that does not exist
- **THEN** the response status is 404 and no book is stored