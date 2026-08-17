## 1. Shared kernel and packages

- [x] 1.1 Add `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` in SharedKernel (`Handle` + `CancellationToken`)
- [x] 1.2 Add the Dapper package to `LibraryService.Modules.Libraries` and `LibraryService.Modules.Books` only

## 2. Libraries slice

- [x] 2.1 Add `Queries/ListLibraries.cs` and `Queries/GetLibrary.cs` (nested Query, Handler, read model; Dapper via `GetDbConnection()`; pass `CancellationToken`)
- [x] 2.2 Add `Commands/CreateLibrary.cs`, `UpdateLibrary.cs`, and `DeleteLibrary.cs` (nested Command/request, Handler, own read model on create; EF only; delete books then library; pass `CancellationToken`)
- [x] 2.3 Add `LibrariesModule` with `AddLibraries` (scoped handler registrations) and `MapLibraries` (`/api/libraries` routes and status codes from `library-management`)

## 3. Books slice

- [x] 3.1 Add `Queries/ListBooks.cs` (own `BookReadModel`; Dapper; `null` when the library is missing; pass `CancellationToken`)
- [x] 3.2 Add `Commands/CreateBook.cs` (own request + `BookReadModel`; EF; `null` when the library is missing; pass `CancellationToken`)
- [x] 3.3 Add `BooksModule` with `AddBooks` and `MapBooks` (`/api/libraries/{libraryId}/books`, `RequireAuthorization`, named list route, `201` via `CreatedAtRoute`)

## 4. Host wiring and cleanup

- [x] 4.1 Register `AddLibraries`, `AddBooks`, and `AddEndpointsApiExplorer` in `Startup.ConfigureServices`; map `MapLibraries` and `MapBooks` next to `MapControllers`
- [x] 4.2 Delete `LibrariesController`, `BooksController`, `BookForm`, and both modules' `Features/` static classes
- [x] 4.3 Confirm Auth production code is untouched and no MediatR (or other mediator) package was added

## 5. Acceptance spec project

- [x] 5.1 Create `tests/LibraryService.Specs` (net8, `WebApplicationFactory`, xUnit, FluentAssertions, EF Sqlite) and add it to the solution
- [x] 5.2 Add `Support/ApiFactory` (isolated open SQLite connection + `LibraryContext` per scenario) and `Support/AuthClient` (admin login / anonymous client)
- [x] 5.3 Remove `tests/LibraryService.Integration.Test` from the solution and delete the project

## 6. Spec coverage

- [x] 6.1 Add `LibraryManagement` specs for every `library-management` scenario (list, get, create, update, delete including books-gone)
- [x] 6.2 Add `BookManagement` specs for every `book-management` scenario (list/create, empty list, missing library, `401`)
- [x] 6.3 Add `UserAuth` specs for every `user-auth` scenario (valid/invalid login, token accepted, missing/invalid token rejected)
- [x] 6.4 Add `cqrs-slices` examples for query side-effect-free GETs and create-then-read on the same database

## 7. Verification

- [x] 7.1 `dotnet build` the solution succeeds
- [x] 7.2 `dotnet test` on `LibraryService.Specs` is green for all scenarios in 6.1–6.4
