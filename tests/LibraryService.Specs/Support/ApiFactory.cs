using LibraryService.Api;
using LibraryService.SharedKernel.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;

namespace LibraryService.Specs.Support;

public sealed class ApiFactory : IAsyncDisposable
{
    private readonly LibraryContext _context;
    private readonly WebApplicationFactory<Program> _factory;

    public HttpClient Client { get; }

    public ApiFactory()
    {
        _context = new LibraryContext(new DbContextOptionsBuilder<LibraryContext>()
            .UseSqlite("DataSource=:memory:")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

        _context.Database.OpenConnection();
        _context.Database.Migrate();

        foreach (var entity in _context.ChangeTracker.Entries().ToList())
            entity.State = EntityState.Detached;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(LibraryContext));
                services.AddSingleton(_context);
            });
        });

        Client = _factory.CreateClient();
    }

    public HttpClient CreateAnonymousClient() => _factory.CreateClient();

    public Task AuthenticateAsAdminAsync() => AuthClient.AuthenticateAsAdminAsync(Client);

    public async Task<LibraryDto> CreateLibraryAsync(string name = "Library Name 1", string location = "Location 1")
    {
        var response = await Client.PostAsJsonAsync("/api/libraries", new { name, location }, Json.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LibraryDto>(Json.Options))!;
    }

    public async Task<BookDto> CreateBookAsync(int libraryId, string name = "Test book 1", string category = "Fiction")
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/libraries/{libraryId}/books",
            new { name, category },
            Json.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookDto>(Json.Options))!;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        await _context.DisposeAsync();
    }
}
