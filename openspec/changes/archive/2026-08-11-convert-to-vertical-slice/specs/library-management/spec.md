## Purpose

Encapsulates the Libraries slice of the LibraryService API: CRUD operations over libraries (list, get by id, create, update, delete) with their HTTP status-code contracts. Deleting a library also removes the books it contains.

## ADDED Requirements

### Requirement: List libraries

The system SHALL return the full list of libraries via `GET /api/libraries`.

#### Scenario: Successful list
- **WHEN** a client sends `GET /api/libraries`
- **THEN** the system responds `200 OK` with a JSON array of libraries

### Requirement: Get a library by id

The system SHALL return a single library via `GET /api/libraries/{libraryId}`.

#### Scenario: Library exists
- **WHEN** a client sends `GET /api/libraries/{libraryId}` for an existing library
- **THEN** the system responds `200 OK` with the library JSON

#### Scenario: Library does not exist
- **WHEN** a client sends `GET /api/libraries/{libraryId}` for a non-existent library
- **THEN** the system responds `404 Not Found`

### Requirement: Create a library

The system SHALL create a library via `POST /api/libraries` and return the created resource.

#### Scenario: Successful create
- **WHEN** a client sends `POST /api/libraries` with a valid library payload
- **THEN** the system creates the library and responds `200 OK` with the created library JSON

### Requirement: Update a library

The system SHALL update an existing library via `PUT /api/libraries/{libraryId}`.

#### Scenario: Library exists
- **WHEN** a client sends `PUT /api/libraries/{libraryId}` for an existing library with a valid payload
- **THEN** the system updates the library and responds `204 No Content`

#### Scenario: Library does not exist
- **WHEN** a client sends `PUT /api/libraries/{libraryId}` for a non-existent library
- **THEN** the system responds `404 Not Found`

### Requirement: Delete a library

The system SHALL delete a library via `DELETE /api/libraries/{libraryId}`, removing its books as well.

#### Scenario: Library exists
- **WHEN** a client sends `DELETE /api/libraries/{libraryId}` for an existing library
- **THEN** the system deletes the library and its books and responds `204 No Content`

#### Scenario: Library does not exist
- **WHEN** a client sends `DELETE /api/libraries/{libraryId}` for a non-existent library
- **THEN** the system responds `404 Not Found`

#### Scenario: Books of deleted library are gone
- **WHEN** a client deletes a library that had books and then requests its books via `GET /api/libraries/{libraryId}/books`
- **THEN** the system responds `404 Not Found` because the library no longer exists
