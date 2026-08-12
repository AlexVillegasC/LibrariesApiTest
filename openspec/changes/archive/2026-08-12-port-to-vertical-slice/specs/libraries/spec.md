## Purpose

Exposes HTTP operations to list, retrieve, create, update, and delete libraries without changing the existing `/api/libraries` routes.

## ADDED Requirements

### Requirement: List all libraries
The system SHALL return every library as JSON with HTTP 200 when a client requests `GET /api/libraries`.

#### Scenario: Libraries exist
- **WHEN** a client sends `GET /api/libraries` and at least one library is stored
- **THEN** the response status is 200 and the body is a JSON array of those libraries

#### Scenario: No libraries exist
- **WHEN** a client sends `GET /api/libraries` and the store is empty
- **THEN** the response status is 200 and the body is an empty JSON array

### Requirement: Get library by id
The system SHALL return a single library for `GET /api/libraries/{libraryId}` when it exists, and SHALL return HTTP 404 when it does not.

#### Scenario: Library found
- **WHEN** a client sends `GET /api/libraries/{libraryId}` for an existing id
- **THEN** the response status is 200 and the body is that library as JSON

#### Scenario: Library missing
- **WHEN** a client sends `GET /api/libraries/{libraryId}` for an id that does not exist
- **THEN** the response status is 404

### Requirement: Create library
The system SHALL persist a new library from `POST /api/libraries` and return HTTP 200 with the created library in the body, including its assigned id.

#### Scenario: Successful create
- **WHEN** a client sends `POST /api/libraries` with a JSON body containing name and location
- **THEN** the library is stored and the response status is 200 with the created library in the body

### Requirement: Update library
The system SHALL update name and location for `PUT /api/libraries/{libraryId}` when the library exists, returning HTTP 204, and SHALL return HTTP 404 when it does not.

#### Scenario: Successful update
- **WHEN** a client sends `PUT /api/libraries/{libraryId}` with a JSON body for an existing library
- **THEN** the stored name and location are updated and the response status is 204

#### Scenario: Update missing library
- **WHEN** a client sends `PUT /api/libraries/{libraryId}` for an id that does not exist
- **THEN** the response status is 404 and no library is created

### Requirement: Delete library
The system SHALL delete an existing library for `DELETE /api/libraries/{libraryId}` and return HTTP 204. Books belonging to that library MUST also be removed. The system SHALL return HTTP 404 when the library does not exist.

#### Scenario: Successful delete
- **WHEN** a client sends `DELETE /api/libraries/{libraryId}` for an existing library that may have books
- **THEN** the library and its books are removed and the response status is 204

#### Scenario: Delete missing library
- **WHEN** a client sends `DELETE /api/libraries/{libraryId}` for an id that does not exist
- **THEN** the response status is 404

#### Scenario: Books of deleted library are gone
- **WHEN** a library has been deleted
- **THEN** a subsequent request for that library's books returns HTTP 404
