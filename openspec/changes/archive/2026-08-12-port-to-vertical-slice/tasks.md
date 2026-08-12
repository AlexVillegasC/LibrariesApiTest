## 1. Shared kernel

- [x] 1.1 Add `Shared/Endpoints/IEndpoint.cs` and an extension that discovers and maps all `IEndpoint` implementations
- [x] 1.2 Move `LibraryContext`, `Book`, and `Library` under `Shared/Data` with namespace `LibraryService.WebAPI`
- [x] 1.3 Move `JwtSettings` under `Shared/Auth` and extract JWT + CORS + Swagger + Npgsql DI into `AddInfrastructure` (or equivalent) so `Startup`/`Program` can call it
- [x] 1.4 Collapse `Startup` into minimal hosting in `Program.cs` (pipeline: migrate, CORS, auth, map feature endpoints)

## 2. Authentication slice

- [x] 2.1 Create `Features/Auth/Login` with request/response, hardcoded credential check, token generation, and `POST /login` (anonymous) returning `{ token }` or 401
- [x] 2.2 Remove `AuthController`, `HackerRank1.Services.AuthenticationService`, and `Helpers/TokenGenerator`

## 3. Libraries slices

- [x] 3.1 Implement `GetAllLibraries` as `GET /api/libraries` returning 200 and a JSON array (empty when none)
- [x] 3.2 Implement `GetLibraryById` as `GET /api/libraries/{libraryId}` returning 200 or 404
- [x] 3.3 Implement `CreateLibrary` as `POST /api/libraries` persisting name/location and returning 200 with the created library (including id)
- [x] 3.4 Implement `UpdateLibrary` as `PUT /api/libraries/{libraryId}` updating name/location (204) or 404 when missing
- [x] 3.5 Implement `DeleteLibrary` as `DELETE /api/libraries/{libraryId}` removing the library and cascaded books (204) or 404 when missing
- [x] 3.6 Remove `LibrariesController` and `ILibrariesService` / `LibrariesService`

## 4. Books slices

- [x] 4.1 Implement `GetBooksByLibrary` as `GET /api/libraries/{libraryId}/books` with `.RequireAuthorization()`, 200 (including empty list) when the library exists, and 404 when it does not
- [x] 4.2 Implement `AddBookToLibrary` as `POST /api/libraries/{libraryId}/books` creating a book (201) or 404 when the library does not exist
- [x] 4.3 Remove `BooksController`, `IBooksService` / `BooksService`, and unused `DTO/BookForm` + `DTO/LibraryForm`

## 5. Tests and cleanup

- [x] 5.1 Update `LibraryService.Integration.Test` to host via `WebApplicationFactory<Program>` (no `UseStartup`), replace pooled Npgsql `DbContext` with the test store, and send a JWT on get-books
- [x] 5.2 Confirm integration tests cover add-book 201/404, get-books 200/404, and delete-library 204/404
- [x] 5.3 Delete leftover layer folders (`Controllers`, `Services`, `DTO`, `Helpers`, `Entities`) and unused `HackerRank1.*` namespaces
