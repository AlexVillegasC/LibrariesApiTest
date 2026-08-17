## Context

See `proposal.md` for motivation. Current constraints that shape the approach:

- Libraries and Books already live in `LibraryService.Modules.*` with `Features/*.cs` static helpers and thin controllers. Auth is a controller + `AuthenticationService`. SharedKernel owns `LibraryContext`, `Book`, `Library`, and Postgres migrations.
- Hosting is still `Startup` + `UseEndpoints` + `MapControllers`. Not converting the host to minimal hosting.
- Integration tests replace the pooled Npgsql context with a **singleton** SQLite `:memory:` `LibraryContext` whose connection is the database. A second connection would see empty data.
- HTTP contracts are defined by `openspec/specs/library-management`, `book-management`, and `user-auth`. Those requirements do not change; `cqrs-slices` adds the read/write split.
- Existing tests do not cover most library CRUD scenarios and deserialize `Book` / `BookForm` from the modules.

## Goals / Non-Goals

**Goals:**
- A CQRS folder layout inside the existing Libraries and Books projects, with injectable handlers and module-mapped endpoints.
- EF-only writes and Dapper reads on the **same** `LibraryContext` connection.
- A new isolated acceptance suite that is the executable form of the three main specs.

**Non-Goals:**
- Auth production rewrite (no `Commands/Login`, no Auth module CQRS).
- MediatR, a home-grown mediator bus, or handler pipelines.
- A second database, replica, cache, or outbox.
- New product behavior (pagination, book update/delete, validation rules, FluentValidation).
- Reqnroll / Gherkin files (OpenSpec remains the specification).
- Replacing `Startup` or dropping `AddControllers` (Auth still needs it).

## Decisions

### D1: Keep one project per slice; add Commands/Queries at the module root

```
LibraryService.Modules.Libraries/
  Commands/   CreateLibrary.cs  UpdateLibrary.cs  DeleteLibrary.cs
  Queries/    ListLibraries.cs  GetLibrary.cs
  LibrariesModule.cs
LibraryService.Modules.Books/
  Commands/   CreateBook.cs
  Queries/    ListBooks.cs
  BooksModule.cs
```

Delete `LibrariesController`, `BooksController`, `BookForm`, and the old `Features/` static classes.

- **Rationale**: The last VSA change already chose compile-time module boundaries. Nesting `Features/<Feature>/` inside a module that *is* the feature is noise.
- **Alternative**: Single API project with `Features/Libraries/...` — rejected; would undo module isolation.

### D2: Handler abstractions in SharedKernel

```csharp
public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
}
```

- **Rationale**: Both slices need the same pair; SharedKernel is already the cross-cutting project. No fourth project.
- **Alternative**: Duplicate interfaces per module — rejected. A `Cqrs` project — rejected as ceremony.

### D3: Nested types, one file per use case

Each file owns `Query` or `Command`, `Handler`, and the request/read models for that use case. ListBooks and CreateBook each define their own `BookReadModel` even if the fields match today (same for library create vs list/get).

Handlers accept `CancellationToken` and pass it to EF (`SaveChangesAsync`, `AnyAsync`, …) and Dapper.

- **Rationale**: Matches the VSA “use case owns everything” rule and the locked file-shape decision.
- **Alternative**: Flat classes in many files — rejected; harder to see a use case.

### D4: Module = DI + endpoint mapping; keep Startup

`LibrariesModule` / `BooksModule` expose `AddLibraries` / `AddBooks` (register each `ICommandHandler` / `IQueryHandler` as scoped) and `MapLibraries` / `MapBooks` (`IEndpointRouteBuilder` groups).

```
Startup.ConfigureServices
  AddLibraries(); AddBooks();
  AddControllers();              // AuthController
  AddEndpointsApiExplorer();     // Swagger sees MapGroup routes

Startup.Configure
  MapControllers();              // POST /login
  MapLibraries();                // /api/libraries
  MapBooks();                    // /api/libraries/{libraryId}/books
```

Books group uses `RequireAuthorization()`. Create-book names the list-books route and returns `TypedResults.CreatedAtRoute` so `201` can still carry a `Location` (tests only assert status).

404 signaling stays as today: `null` from get/list-books/create-book, `false` from update/delete. No `Result<T>`.

- **Rationale**: Endpoint mapping was requested; retiring controllers avoids duplicate routes. Keeping `Startup` matches the previous VSA non-goal.
- **Alternative**: Keep controllers that inject handlers — rejected; user asked for module mapping. Carter — rejected; extra library.

### D5: EF writes, Dapper reads, same connection

Commands use `LibraryContext` DbSets and `SaveChangesAsync`. Queries use Dapper on `context.Database.GetDbConnection()` against tables `Libraries` and `Books`. Dapper is referenced from the Libraries and Books projects only.

Delete library still loads/removes books then the library in one EF `SaveChanges` (explicit, not relying on FK cascade) so SQLite tests and Postgres stay aligned.

- **Rationale**: `GetDbConnection()` is the same connection the test factory keeps open. A separate `IDbConnection` factory would break `:memory:` unless we redesign the harness.
- **Alternative**: Dapper on writes — forbidden. Separate read DB — forbidden.

### D6: Wire-compatible models, not shared types

| Operation | HTTP | Handler result |
|---|---|---|
| ListLibraries | `GET /api/libraries` | `IReadOnlyList<LibraryReadModel>` |
| GetLibrary | `GET /api/libraries/{libraryId}` | `LibraryReadModel?` |
| CreateLibrary | `POST /api/libraries` body `{ name, location }` | `LibraryReadModel` → `200` |
| UpdateLibrary | `PUT /api/libraries/{libraryId}` | `bool` → `204` / `404` |
| DeleteLibrary | `DELETE /api/libraries/{libraryId}` | `bool` → `204` / `404` |
| ListBooks | `GET .../books` + JWT | `IReadOnlyList<BookReadModel>?` |
| CreateBook | `POST .../books` body `{ name, category }` + JWT | `BookReadModel?` → `201` / `404` |

JSON field names stay camelCase `id`, `name`, `location` / `category`, `libraryId`. Create bodies omit `id` (and book `libraryId`); extra JSON is ignored. Null-coalesce empty strings as today. No new validation.

### D7: Replace the test project with OpenSpec-aligned specs

Delete `tests/LibraryService.Integration.Test`. Add `tests/LibraryService.Specs`:

```
tests/LibraryService.Specs/
  Support/          ApiFactory, AuthClient
  LibraryManagement/  one class per requirement, one test per scenario
  BookManagement/
  UserAuth/
```

No Reqnroll. Class/method names copy the main spec requirement/scenario titles. Each test is Given/When/Then over HTTP. Each scenario gets its own open SQLite connection + `LibraryContext` so Dapper and EF share data and tests do not leak rows.

Cover **all** scenarios in `library-management`, `book-management`, and `user-auth`, plus the side-effect and create-then-read scenarios in `cqrs-slices`. Do not assert Dapper vs EF. Drop the swagger-only smoke test (not in the specs). Seed through HTTP (or a support helper) so When/Then stay on the API.

- **Rationale**: OpenSpec is already the Gherkin. A second `.feature` layer would drift. Isolated DB is the actual BDD upgrade over the singleton context.
- **Alternative**: Keep and patch the old project — rejected. Reqnroll — rejected (two sources of truth).

## Risks / Trade-offs

- **[Risk] Dapper + SQLite `:memory:` sees an empty DB** → Mitigation: queries only use `GetDbConnection()`; each spec scenario keeps that connection open for its lifetime.
- **[Risk] Minimal APIs missing from Swagger** → Mitigation: `AddEndpointsApiExplorer()`; routes stay the same paths.
- **[Risk] `CreatedAtAction` Location header changes** → Mitigation: named list-books route + `CreatedAtRoute`. Specs/tests do not require the header.
- **[Risk] POST library with `id` in the body behaved like binding `Library`** → Mitigation: new request type ignores `id`. No current test or spec sends it.
- **[Risk] Cross-slice rules (library exists, delete books with library)** → Mitigation: keep them inside the use case against SharedKernel tables; do not add a Books service dependency.
- **[Trade-off] Query handlers still take `LibraryContext`** → Acceptable: it is a connection source, not an EF query path. Avoids a factory the tests cannot satisfy.

## Migration Plan

1. Add handler interfaces to SharedKernel; add Dapper to the two data modules.
2. Add command/query files and `*Module` classes; wire `Startup`; delete old controllers/`Features`/`BookForm`.
3. Replace the test project and solution entries.
4. Gate: `dotnet build` and `dotnet test` on `LibraryService.Specs` — every listed scenario green.
5. Rollback: revert the change branch; production schema is unchanged (no new migrations).
