## Why

The solution is a single horizontal-layered monolith (`Controllers/` → `Services/` → `Data/`) with split-brain namespaces (`LibraryService.WebAPI.*` vs `HackerRank1.*`), several endpoints stubbed out with `NotImplementedException`, and two stale/abandoned project scaffolds (`IntegrationTest/`, empty `src/` clean-arch layout) that add noise without value. The existing integration tests are red because they exercise the missing endpoints. Converting to Vertical Slice Architecture (VSA) fixes the structure, unifies namespaces, and — as part of the reorganization — completes the missing operations so the test suite goes green.

## What Changes

- **BREAKING**: Replace the `HackerRank1` monolith project with a multi-project VSA layout:
  - One project per slice: `src/Modules/Libraries`, `src/Modules/Books`, `src/Modules/Auth`.
  - A `Shared Kernel` project for cross-cutting concerns (`LibraryContext`, entities, JWT settings, base primitives).
  - The web host (`HackerRank1.API`) composes the slices and owns `Program`/`Startup`, auth/JWT config, CORS, Swagger.
- **BREAKING**: Unify namespaces to `LibraryService.*` everywhere; remove the `HackerRank1.*` namespace split.
- **BREAKING**: Remove stale artifacts: delete `IntegrationTest/` (net6 ghost, not in solution) and the empty `src/` clean-arch scaffold; keep and migrate `LibraryService.Integration.Test` into the new layout.
- Implement the missing behavior that tests depend on:
  - `POST /api/libraries/{libraryId}/books` → 201 Created.
  - `DELETE /api/libraries/{id}` → 204 No Content (404 when missing).
  - Complete `LibrariesService.Delete`, `BooksService.Add/Update/Delete` (remove `NotImplementedException`).
  - `DELETE /api/libraries/{id}/books` and `DELETE /api/books/{id}` only if the existing test suite requires them; otherwise keep current surface and document it.
- Keep the existing externally observable API surface (routes, DTO shapes, status codes, JWT auth on books) unchanged except for the endpoints added above.
- Keep `dotnet build` + `dotnet test` green as the acceptance gate.

## Capabilities

### New Capabilities

- `library-management`: Libraries CRUD — list, get by id, create, update, and (new) delete. Encapsulates the Libraries slice behavior and status-code contracts.
- `book-management`: Books within a library — list books of a library (auth required), create book in a library (new), and the book/libraries relationship rules (404 on missing library, 201 on create).
- `user-auth`: JWT login — `POST /login` authenticating admin credentials and issuing a signed token used to authorize books endpoints.

### Modified Capabilities

<!-- No existing specs; the specs/ directory is currently empty. -->

## Impact

- **Project layout**: `HackerRank1.sln` updated; `HackerRank1/` project removed; new projects added under `src/`; test project relocated/renamed.
- **Code**: All controllers/services/DTOs/entities/migrations move into their slice or the shared kernel. Namespaces change across the codebase.
- **Dependencies**: Test project packages upgraded from net6-era (`Microsoft.AspNetCore.Mvc.Testing` 6.0.0, EF InMemory/Sqlite 6.0.0) to net8-compatible versions. Main project keeps net8.0.
- **Data**: EF Core migrations preserved; `LibraryContext` (Libraries/Books entities) relocates to the shared kernel. Postgres/Supabase connection and JWT settings unchanged.
- **Systems**: External API surface, Swagger, CORS (Vite :5173), and JWT config unchanged except new endpoints listed above.
