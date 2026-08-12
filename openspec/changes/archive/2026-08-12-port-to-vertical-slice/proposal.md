## Why

The LibraryService API is organized by technical layer (Controllers, Services, DTO, Data, Helpers), so a single use case is split across folders and fat service interfaces. Porting to Vertical Slice Architecture groups each HTTP operation with its request, handler, and persistence so features can be completed, tested, and changed independently.

## What Changes

- Reorganize the ASP.NET Core 8 API from horizontal layers into feature slices (one folder per use case).
- Replace `ILibrariesService` / `IBooksService` catch-all services with per-slice handlers that use `LibraryContext` directly.
- Keep existing HTTP routes and status codes so current clients and integration tests remain valid.
- Complete stubbed operations that tests already expect: add a book to a library, delete a library, and return 404 when listing books for a missing library.
- Keep JWT login and `[Authorize]` on listing books; move auth-specific types into the login slice and shared JWT infrastructure.
- Retire unused layer DTOs (`BookForm`, `LibraryForm` as global types) in favor of per-slice request/response models.
- Leave EF Core entities, `LibraryContext`, PostgreSQL, migrations, CORS, and Swagger as shared infrastructure.

## Capabilities

### New Capabilities

- `libraries`: List, get, create, update, and delete libraries over the existing `/api/libraries` routes.
- `books`: List and add books nested under a library (`/api/libraries/{libraryId}/books`), including 404 when the library does not exist.
- `authentication`: Anonymous login at `/login` issuing a JWT; listing books requires a valid token.

### Modified Capabilities

- None. There are no existing main specs under `openspec/specs/`.

## Impact

- **Code**: `HackerRank1` controllers, services, DTOs, helpers, and `Startup` DI registrations. Shared pieces remain `LibraryContext`, `Book`, `Library`, JWT middleware, and Swagger.
- **APIs**: Same public routes (`/api/libraries`, `/api/libraries/{id}`, `/api/libraries/{id}/books`, `/login`). Completing stubs changes runtime behavior from 500/missing endpoint to the status codes integration tests already assert (201, 204, 404).
- **Tests**: `LibraryService.Integration.Test` stays the contract for add-book, get-books, and delete-library. Tests may need DI/hosting updates if `Startup` is replaced by minimal hosting.
- **Dependencies**: No new packages required for the slice layout. MediatR / FastEndpoints are out of scope.
- **Data**: PostgreSQL schema and existing migrations are unchanged.
