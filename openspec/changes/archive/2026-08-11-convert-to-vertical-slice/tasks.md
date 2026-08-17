## 1. Scaffold VSA projects

- [x] 1.1 Create `src/LibraryService.SharedKernel/` (net8.0 class library) with csproj
- [x] 1.2 Create `src/Modules/LibraryService.Modules.Libraries/` with csproj referencing SharedKernel
- [x] 1.3 Create `src/Modules/LibraryService.Modules.Books/` with csproj referencing SharedKernel
- [x] 1.4 Create `src/Modules/LibraryService.Modules.Auth/` with csproj (self-contained, no SharedKernel dependency)
- [x] 1.5 Create `src/HackerRank1.API/` (net8.0 web) referencing the three modules and SharedKernel; move `Program.cs`/`Startup.cs` here
- [x] 1.6 Restore EF packages in SharedKernel (`Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, Design/Tools) and web packages in the API (`JwtBearer`, `Swashbuckle.AspNetCore`)

## 2. Move code into slices and unify namespaces

- [x] 2.1 Move `LibraryContext`, `Book`, `Library`, and `Migrations/` into SharedKernel; update namespaces to `LibraryService.SharedKernel.Data` and fix `using`s (design D1)
- [x] 2.2 Move `LibrariesController` + libraries logic into the Libraries module as `Features/` (design D2), namespace `LibraryService.Modules.Libraries`
- [x] 2.3 Move `BooksController` + books logic into the Books module as `Features/` (design D2), namespace `LibraryService.Modules.Books`
- [x] 2.4 Move `AuthController`, `AuthenticationService`, `TokenGenerator`, `JwtSettings`, `User` DTO into the Auth module, namespace `LibraryService.Modules.Auth`
- [x] 2.5 Initialize non-nullable entity string properties (`Book.Name/Category`, `Library.Name/Location`) with `string.Empty` (design D6)
- [x] 2.6 Remove `MSTest.TestFramework` package and any `MSTest` usings from the API project (design D6)

## 3. Implement missing behavior (design D3)

- [x] 3.1 Implement `LibrariesService.Delete` (or feature equivalent): verify library exists, delete its books first, then the library (no `NotImplementedException` anywhere)
- [x] 3.2 Add `DELETE /api/libraries/{libraryId}` to the Libraries controller: `204 No Content` on success, `404 Not Found` when missing (spec `library-management` — Delete a library)
- [x] 3.3 Implement `BooksService.Add/Update/Delete` (design D3); `Add` returns the created book
- [x] 3.4 Add `POST /api/libraries/{libraryId}/books` to the Books controller: `201 Created` + created book, `404 Not Found` when the library is missing (spec `book-management` — Create a book)
- [x] 3.5 Update `GET /api/libraries/{libraryId}/books` to return `404 Not Found` when the library does not exist (spec `book-management` — List books)

## 4. Update solution and remove stale artifacts

- [x] 4.1 Rewrite `HackerRank1.sln` to reference the new projects; remove `HackerRank1/` project entry
- [x] 4.2 Delete `HackerRank1/` project directory
- [x] 4.3 Delete `IntegrationTest/` ghost project (net6, not in solution)
- [x] 4.4 Delete the empty `src/` clean-arch scaffold leftovers (old `src/HackerRank1.{API,Application,Domain,Infrastructure}` dirs containing only `obj/`)

## 5. Migrate and upgrade the test project

- [x] 5.1 Move `LibraryService.Integration.Test` to `tests/`, target net8.0, update ProjectReference to the new API project
- [x] 5.2 Upgrade test packages to net8-era: `Microsoft.AspNetCore.Mvc.Testing` 8.x, `Microsoft.EntityFrameworkCore.{Sqlite,InMemory}` 8.x, coherent FluentAssertions/xunit
- [x] 5.3 Add an auth helper that logs in via `POST /login` (admin/1234) and returns a `Bearer` token (design D4)
- [x] 5.4 Update existing tests to attach the `Authorization: Bearer` header on books requests; keep the SQLite `:memory:` singleton `DbContext` pattern
- [x] 5.5 Ensure no `NotImplementedException` or stale `MSTest` references remain in the test project

## 6. Verification gate

- [x] 6.1 Run `dotnet build` on the solution — must succeed with no new warnings
- [x] 6.2 Run `dotnet test` — all integration tests green (spec scenarios: list/get/create/update/delete libraries, list/create books, 404/401 cases)
- [x] 6.3 Smoke-check `POST /login`, books list with/without token, and Swagger endpoint behavior unchanged
