## Context

See proposal.md for motivation. The API is a single ASP.NET Core 8 project (`HackerRank1`) using classic `Startup` + MVC controllers. Domain types live in `LibraryContext` (`Library`, `Book`). Services talk to EF Core directly; there is no repository layer. `BookForm` / `LibraryForm` are unused by controllers. Book add/update/delete and library delete are stubs; integration tests already assert add-book (201/404), get-books (200/404), and delete-library (204/404). Auth is hardcoded (`admin` / `1234`) with JWT on get-books only. PostgreSQL + existing migrations stay.

Constraints: keep public routes; no MediatR/FastEndpoints; complete the stubbed operations the tests already describe.

## Goals / Non-Goals

**Goals:**
- Organize by use case (feature folder per HTTP operation) instead of by layer.
- Each slice owns its endpoint, request/response types, and handler logic.
- Shared kernel limited to EF Core, JWT middleware/config, CORS, Swagger, and endpoint discovery.
- Finish add-book, delete-library, and get-books 404-when-library-missing as part of the port.
- Unify namespaces to `LibraryService.WebAPI` (drop mixed `HackerRank1.*`).

**Non-Goals:**
- New NuGet packages (MediatR, FastEndpoints, Carter, AutoMapper).
- Generic repository or unit-of-work abstractions.
- HTTP endpoints for book update/delete (service stubs exist; no routes or tests).
- Replacing hardcoded login with a user store.
- Changing PostgreSQL schema or rewriting migrations.
- Expanding JWT to other routes.

## Decisions

### 1. Feature folders + REPR, not MediatR

Each use case lives under `Features/<Area>/<UseCase>/` with a request, endpoint, and handler (one file is fine when the slice is small).

```
HackerRank1/
  Features/
    Auth/Login/
    Libraries/
      GetAllLibraries/
      GetLibraryById/
      CreateLibrary/
      UpdateLibrary/
      DeleteLibrary/
    Books/
      GetBooksByLibrary/
      AddBookToLibrary/
  Shared/
    Data/          LibraryContext, Book, Library
    Auth/          JwtSettings, JWT DI + middleware helpers
    Endpoints/     IEndpoint + MapFeatureEndpoints
  Program.cs
```

**Why:** Eight endpoints. MediatR adds pipeline and handler registration without a current need for behaviors. REPR keeps the slice readable in one place.

**Alternatives:** MediatR (more ceremony); keep fat controllers grouped by area (weaker isolation); FastEndpoints (extra dependency).

### 2. Minimal APIs + `IEndpoint` discovery

Replace MVC controllers with Minimal API endpoints. Each slice implements:

```
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

`Program` (or a `Shared/Endpoints` extension) scans and maps all `IEndpoint` implementations. Get-books uses `.RequireAuthorization()`; login uses `.AllowAnonymous()`; other routes stay anonymous.

**Why:** .NET 8 native, no controller base class, easy per-route auth. Tests host through `WebApplicationFactory<Program>`, so moving off `UseStartup<Startup>()` is acceptable if the factory is updated.

**Alternatives:** Keep `[ApiController]` classes inside each slice (smaller hosting change, more leftover MVC). Chosen: Minimal APIs because the port already touches every endpoint.

### 3. No shared application services; DbContext in the slice

Delete `ILibrariesService` / `IBooksService`. Handlers inject `LibraryContext`. Duplicate "library exists?" checks in get-books and add-book (or a one-line `LibraryContext` extension in `Shared/Data`). Cascade delete of books stays on the existing FK (`OnDelete.Cascade`).

**Why:** The current services are generic CRUD facades (`Get(int[] ids)`). VSA prefers a query shaped for one use case over a shared service that every controller calls.

**Alternatives:** Keep services as "application layer" behind slices (still horizontal). Repositories (no current benefit; EF already is the abstraction).

### 4. Per-slice contracts; retire global forms

`CreateLibrary` / `UpdateLibrary` / `AddBookToLibrary` define their own request records. `BookForm` and `LibraryForm` go away. JSON property names stay camelCase (`id`, `name`, `location`, `category`, `libraryId`) so existing clients and Newtonsoft-based tests still deserialize.

Create-library keeps HTTP 200 (current `Ok(l)`), not 201 — tests do not cover create-library status, and the proposal is not a breaking API change.

### 5. Auth stays in the login slice + shared JWT setup

`AuthenticationService` (hardcoded credentials) and `TokenGenerator` move into `Features/Auth/Login/`. `JwtSettings` and `AddJwtBearer` stay in `Shared/Auth` because middleware is cross-cutting. Login response remains `{ "token": "..." }`.

### 6. Hosting: collapse `Startup` into `Program` + extension methods

Move DI and pipeline from `Startup` into `Program.cs` with `AddInfrastructure()` / `AddFeatureEndpoints()` so slice registration is obvious. Auto-migrate on startup remains.

**Why:** `IEndpoint` mapping fits the minimal hosting model. Integration tests that call `UseStartup<Startup>()` must switch to the default `WebApplicationFactory<Program>` and replace `DbContext` via `ConfigureServices` / `ConfigureTestServices`.

### 7. Slice inventory (from current surface + tests)

| Slice | Route | Completes stub? |
|---|---|---|
| Login | `POST /login` | no |
| GetAllLibraries | `GET /api/libraries` | no |
| GetLibraryById | `GET /api/libraries/{libraryId}` | no |
| CreateLibrary | `POST /api/libraries` | no |
| UpdateLibrary | `PUT /api/libraries/{libraryId}` | no |
| DeleteLibrary | `DELETE /api/libraries/{libraryId}` | yes |
| GetBooksByLibrary | `GET /api/libraries/{libraryId}/books` | yes (404 if library missing) |
| AddBookToLibrary | `POST /api/libraries/{libraryId}/books` | yes |

Book update/delete stay unimplemented and unrouted.

## Risks / Trade-offs

- **[Risk] Integration tests still assume `Startup` and SQLite swap of `LibraryContext`** → Update the test host to `WebApplicationFactory<Program>` and replace the pooled Npgsql `DbContext` registration (remove `DbContextOptions<LibraryContext>` as well as the context type).
- **[Risk] Get-books currently returns 200 for unknown library ids; tests expect 404** → Treat 404 as required behavior of the new slice, not a regression.
- **[Risk] Get-books is `[Authorize]` but tests do not send a JWT** → Tests will fail until they login first or the test host disables auth. Prefer logging in (or a test auth handler) rather than removing authorization.
- **[Risk] Duplicated "library exists" checks** → Accept small duplication; extract a `LibraryContext` extension only if a third slice needs it.
- **[Trade-off] No MediatR** → No shared pipeline (logging/validation behaviors). Add later per-slice or via Minimal API filters if needed.
- **[Trade-off] Shared entities in `LibraryContext` file** → Entities stay shared because they are the persistence model, not a layer to slice.

## Migration Plan

1. Add `Shared/` (`IEndpoint`, JWT helpers, move `LibraryContext`) without changing routes.
2. Add feature folders and map new endpoints alongside old controllers (or replace one area at a time: Auth → Libraries → Books).
3. Implement DeleteLibrary, AddBookToLibrary, and GetBooksByLibrary 404 in their slices.
4. Remove controllers, layer services, and unused DTO folder.
5. Collapse `Startup` into `Program`; fix integration test host and auth.
6. Rollback: revert the branch; schema is unchanged so no DB rollback is required.

## Open Questions

None. Hardcoded credentials, 200-on-create, and JWT-only-on-get-books are existing behavior to preserve, not decisions to defer.
