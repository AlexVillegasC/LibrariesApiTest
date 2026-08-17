## Why

Libraries and Books are already vertical slices, but every operation still shares EF Core, domain entities, and static helpers invoked from controllers. That hides the read/write split this lab is meant to demonstrate and keeps tests coupled to a shared in-memory context and module DTOs. The slices need CQRS — commands and queries as independent use cases, no mediator — while the existing HTTP contracts stay intact.

## What Changes

- Reorganize the Libraries and Books modules into `Commands/` and `Queries/`, one file per use case, with nested `Query`/`Command`/`Handler`/models.
- Introduce small `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` abstractions in SharedKernel. Endpoints resolve the handler through DI and call `Handle` directly. **No MediatR or other mediator.**
- Replace `LibrariesController` and `BooksController` with `LibrariesModule` / `BooksModule` that register handlers and map endpoints. Auth stays on `AuthController`.
- Commands persist with EF Core on the existing `LibraryContext`. Queries read with Dapper (or raw SQL) via `LibraryContext.Database.GetDbConnection()` and return dedicated read models — not `Book` / `Library` entities. Command and query models are not shared.
- **BREAKING** (test project only): delete `tests/LibraryService.Integration.Test` and add `tests/LibraryService.Specs` — OpenSpec-aligned, isolated-per-scenario acceptance tests. No Reqnroll; the main specs remain the specification.
- Auth production code, routes, status codes, JSON field names, JWT on books, and the single application database are unchanged.

## Capabilities

### New Capabilities

- `cqrs-slices`: Command/query separation for the Libraries and Books slices — direct handler execution, EF writes, Dapper reads, dedicated models, and module-mapped endpoints.

### Modified Capabilities

- None. `library-management`, `book-management`, and `user-auth` HTTP requirements stay as they are in `openspec/specs/`. This change re-implements those behaviors; it does not alter them.

## Impact

- **Code**: `LibraryService.Modules.Libraries`, `LibraryService.Modules.Books`, SharedKernel (handler interfaces + Dapper package), `Startup` composition. Controllers and `BookForm` in those two modules are removed.
- **APIs**: External routes, status codes, and JSON shapes unchanged (`/api/libraries`, `/api/libraries/{id}/books`, `/login`). Swagger must still list the remapped endpoints (`AddEndpointsApiExplorer`).
- **Tests**: New `LibraryService.Specs` project; solution file updated; old integration project removed. Gate is every scenario in the three main specs, not the old xUnit class.
- **Data**: Same Postgres/SQLite database. No replica, cache, or second store.
- **Auth**: Unchanged. Books endpoints still require JWT; the new spec suite still covers `user-auth`.
