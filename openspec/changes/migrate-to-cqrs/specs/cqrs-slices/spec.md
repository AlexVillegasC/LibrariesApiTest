## Purpose

Separates Libraries and Books into independent command and query use cases so reads and writes can evolve without sharing models or a mediator, while keeping the existing HTTP contracts.

## ADDED Requirements

### Requirement: Independent command and query use cases

The system SHALL implement each Libraries and Books operation as its own use case that owns the request and response models for that operation. Command use cases SHALL change persisted state. Query use cases SHALL only return data.

#### Scenario: Library writes are commands
- **WHEN** a client creates, updates, or deletes a library
- **THEN** the system handles that request as a command use case that persists the change

#### Scenario: Library reads are queries
- **WHEN** a client lists libraries or gets a library by id
- **THEN** the system handles that request as a query use case that returns data without persisting changes

#### Scenario: Book write is a command
- **WHEN** an authenticated client creates a book in a library
- **THEN** the system handles that request as a command use case that persists the book

#### Scenario: Book read is a query
- **WHEN** an authenticated client lists books of a library
- **THEN** the system handles that request as a query use case that returns data without persisting changes

### Requirement: Queries have no side effects

A Libraries or Books query SHALL NOT create, update, or delete persisted data.

#### Scenario: Listing libraries does not change data
- **WHEN** a client sends `GET /api/libraries` twice
- **THEN** both responses are `200 OK` and the library collection is unchanged between the two calls

#### Scenario: Listing books does not change data
- **WHEN** an authenticated client sends `GET /api/libraries/{libraryId}/books` twice for an existing library
- **THEN** both responses are `200 OK` and the book collection for that library is unchanged between the two calls

### Requirement: Writes persist to the application database

Library and book commands SHALL persist changes to the existing application database. The system SHALL NOT introduce a separate read database, replica, or cache as part of this capability.

#### Scenario: Created library is readable
- **WHEN** a client creates a library and then lists libraries
- **THEN** the created library appears in the list

#### Scenario: Created book is readable
- **WHEN** an authenticated client creates a book in a library and then lists that library's books
- **THEN** the created book appears in the list

### Requirement: Command and query models evolve independently

The system SHALL NOT reuse a single shared DTO as both a command request and a query response simply because the fields currently match. Query responses SHALL NOT be the persisted domain entity types.

#### Scenario: Library create request is not the list response type
- **WHEN** a client creates a library and then lists libraries
- **THEN** both operations succeed with the same JSON field names (`id`, `name`, `location`) and the create body type is not required to be the list item type

#### Scenario: Book create request is not the list response type
- **WHEN** an authenticated client creates a book and then lists that library's books
- **THEN** both operations succeed with the same JSON field names (`id`, `name`, `category`, `libraryId`) and the create body type is not required to be the list item type

### Requirement: Direct use-case execution

Libraries and Books endpoints SHALL invoke the matching command or query use case directly. The system SHALL NOT introduce a mediator library to dispatch those operations.

#### Scenario: Endpoint executes the use case
- **WHEN** a client calls a Libraries or Books endpoint
- **THEN** the host resolves and executes the matching use case without a mediator pipeline

### Requirement: Existing HTTP contracts remain the source of truth

Libraries and Books endpoints SHALL preserve the routes, status codes, authentication rules, and JSON field names defined by `library-management` and `book-management`. Auth production behavior defined by `user-auth` SHALL remain unchanged.

#### Scenario: Library contracts unchanged
- **WHEN** a client exercises library list, get, create, update, and delete
- **THEN** the responses match the `library-management` scenarios (including `404` on missing get/update/delete and deleting a library's books)

#### Scenario: Book contracts unchanged
- **WHEN** an authenticated client exercises book list and create
- **THEN** the responses match the `book-management` scenarios (including `201` on create, `404` on missing library, and `401` without a valid JWT)
