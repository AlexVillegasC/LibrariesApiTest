## Context

See `proposal.md` — Why. Current state that shapes this design:

- Single net8.0 project `HackerRank1/` with horizontal layering (`Controllers/`, `Services/`, `Data/`, `DTO/`, `Entities/`, `Helpers/`) and split namespaces (`LibraryService.WebAPI.*` vs `HackerRank1.*`).
- Stubbed code: `LibrariesService.Delete`, `BooksService.Add/Update/Delete` all `throw new NotImplementedException()`; `BooksController` exposes only GET; `LibrariesController` has no DELETE.
- Integration tests (`LibraryService.Integration.Test`, xunit + `WebApplicationFactory`, SQLite `:memory:`) are red because they call the missing endpoints, and they do not send JWT tokens despite `[Authorize]` on books GET.
- Stale artifacts: `IntegrationTest/` (net6, not in solution) and an empty `src/` clean-arch scaffold with only `obj/` folders.
- EF Core 8 + Npgsql against Supabase; migrations exist. `Program` uses `Startup` (non-minimal hosting).

## Goals / Non-Goals

**Goals:**
- A buildable VSA layout: one project per slice, a shared kernel for cross-cutting data/auth primitives, and a thin API host.
- Unify namespaces under `LibraryService.*`.
- Complete the missing endpoints/operations so the migrated integration suite is green on `dotnet test`.
- Keep external routes, DTO shapes, status codes, CORS, and JWT config stable except the two new routes (POST books, DELETE library).

**Non-Goals:**
- No new features beyond the missing endpoints required by specs/tests (no books update/delete routes, no pagination, no real user store).
- No auth re-architecture: the hardcoded admin credential stays.
- Not converting to minimal hosting or removing `Startup`.
- Not preserving the stale projects — they are deleted.

## Decisions

### D1: Project layout

```
HackerRank1.sln
├── src/
│   ├── HackerRank1.API/                  # web host: Program, Startup, DI composition
│   │                                     # JWT, CORS, Swagger, DbContext registration
│   ├── Modules/
│   │   ├── LibraryService.Modules.Libraries/   # Libraries slice
│   │   ├── LibraryService.Modules.Books/       # Books slice
│   │   └── LibraryService.Modules.Auth/        # Auth slice (login, TokenGenerator, JwtSettings)
│   └── LibraryService.SharedKernel/            # LibraryContext, entities, migrations
└── tests/
    └── LibraryService.Integration.Test/        # migrated + upgraded (net8)
```

- Each module project is self-contained: its controller plus feature-focused handlers live together, owning everything needed for its vertical slice.
- The host project is the only one that references all modules; it owns DI registration, auth pipeline, CORS, Swagger.
- **Rationale**: multi-project slices give compile-time boundaries (a slice can't reach into another slice's internals) and match the requested "multi-project VSA". Alternatives: single project with feature folders (rejected — weaker boundaries, no compile-time isolation), reuse of the old `src/HackerRank1.*` names (rejected — those folders are empty ghosts).
- Namespaces: `LibraryService.Api`, `LibraryService.Modules.<Slice>`, `LibraryService.SharedKernel.*` (data in `LibraryService.SharedKernel.Data`).

### D2: Slice-internal shape (feature folders + thin controller)

Each module uses feature folders. Example for Books:

```
LibraryService.Modules.Books/
├── BooksController.cs                      # thin routing, delegates per-feature
└── Features/
    ├── ListBooks/ListBooks.cs              # query handler → list of books
    └── CreateBook/CreateBook.cs            # command handler → 201/404
```

- Controllers stay thin: parse route/DTO, call a feature operation, map result to status code. Business/data logic lives in the feature.
- **Rationale**: classic VSA ("controllers and logic for one feature co-located"). Alternatives: one controller per feature (purist, rejected — over-fragmentation for this small API); keeping horizontal `Services/` (rejected — that's the layering we are leaving).

### D3: Completing the missing behavior

- **POST `/api/libraries/{libraryId}/books`** → verify the library exists (else 404), add the book, return `201 Created` + created book JSON.
- **DELETE `/api/libraries/{libraryId}`** → verify existence (else 404), remove the library's books first, then the library, return `204 No Content`.
  - Books are deleted explicitly before the library rather than relying on FK cascade, so behavior is identical across SQLite (test, `EnsureCreated`) and Postgres (prod, migrations) regardless of how the FK cascade is configured in the existing migration.
- **`BooksService` ops**: implement Add/Update/Delete (no `NotImplementedException` anywhere). Only Add is reachable via routes.
- **`BooksController.GetAll`**: now returns `404 Not Found` when the library doesn't exist (currently returns `200 []`), matching the spec and tests.

### D4: Auth in the migrated tests

Books endpoints keep `[Authorize]`. The migrated tests will obtain a token via `POST /login` (admin/1234) and attach `Authorization: Bearer <token>` before hitting books endpoints.

- **Rationale**: preserves the current authenticated surface instead of weakening it to fit the tests. The test change is part of the migration since the tests are being moved/upgraded anyway.
- **Alternative considered**: dropping `[Authorize]` so tests pass unmodified — rejected, it would remove real behavior.

### D5: Test project upgrade

Move `LibraryService.Integration.Test` to `tests/`, target net8.0, upgrade packages to net8-era (`Microsoft.AspNetCore.Mvc.Testing` 8.x, `Microsoft.EntityFrameworkCore.{Sqlite,InMemory}` 8.x, FluentAssertions, xunit). Keep the existing `WebApplicationFactory` + shared in-memory SQLite `DbContext` pattern.

### D6: Dependency and hygiene cleanup

- Remove `MSTest.TestFramework` from the API project (it is unused there) and the stray `Using Microsoft.VisualStudio.TestTools.UnitTesting` from the test project.
- `Newtonsoft.Json` stays (used by tests for serialization/deserialization).
- Non-nullable `Book.Name`/`Book.Category` and `Library.Name`/`Library.Location` get initialized (e.g., `string.Empty`) to satisfy `Nullable` warnings cleanly.
- `TokenGenerator`, `JwtSettings`, `User` DTO, and the `/login` route move into the Auth module unchanged.

## Risks / Trade-offs

- **Namespace/rename churn across the whole codebase** → Mechanical, verified by a full `dotnet build`; migrations only reference entity types by name, so they need namespace updates too.
- **Migration vs SQLite model drift** → The delete flow explicitly deletes books before the library, so FK cascade configuration does not affect correctness on either provider.
- **Tests share one singleton `DbContext`** → Preserve the existing pattern (single in-memory connection, `Detached` cleanup) when migrating; it is already proven in the current suite.
- **Auth assumption**: tests must authenticate → If reviewers prefer unauthenticated books endpoints, that changes the `book-management`/`user-auth` specs and test setup, not the architecture.
- **FluentAssertions 5.0.0 + xunit 2.9.3 mismatch in ghost project** → Ghost project is deleted; the kept test project uses coherent versions.

## Migration Plan

1. Scaffold new projects (`src/HackerRank1.API`, `src/Modules/*`, `src/SharedKernel`, `tests/*`) with csproj references.
2. Move code file-by-file into slices (entities/context/migrations → SharedKernel; auth → Auth module; books → Books module; libraries → Libraries module; host wiring → API).
3. Unify namespaces; fix `using` statements; implement the missing operations from D3.
4. Rewrite solution file to reference the new projects; delete `HackerRank1/`, `IntegrationTest/`, and the empty `src/` scaffold.
5. Migrate + upgrade the test project (D4/D5); add auth helper.
6. Gate: `dotnet build` (0 warnings as feasible) and `dotnet test` green.
7. Rollback if needed: the old layout lives in git history (commit `01d25f1` / `8f936a9`), so this is a normal reversible refactor.
