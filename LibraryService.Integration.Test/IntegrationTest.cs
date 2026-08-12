using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using LibraryService.WebAPI;
using LibraryService.WebAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace LibraryService.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly SqliteConnection _connection;
        private readonly LibraryContext context;

        public HttpClient Client { get; private set; }

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            context = new LibraryContext(new DbContextOptionsBuilder<LibraryContext>()
                .UseSqlite(_connection)
                .EnableSensitiveDataLogging()
                .Options);

            Client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    RemoveLibraryContext(services);
                    services.AddDbContext<LibraryContext>(options => options.UseSqlite(_connection));
                });
            }).CreateClient();

            context.Database.EnsureCreated();
            Authenticate().GetAwaiter().GetResult();
        }

        private static void RemoveLibraryContext(IServiceCollection services)
        {
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(LibraryContext) ||
                    d.ServiceType == typeof(DbContextOptions<LibraryContext>) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(LibraryContext))) ||
                    (d.ImplementationType != null && d.ImplementationType.IsGenericType
                        && d.ImplementationType.GenericTypeArguments.Contains(typeof(LibraryContext))))
                .ToList();

            foreach (var descriptor in descriptors)
                services.Remove(descriptor);
        }

        private async Task Authenticate()
        {
            var response = await Client.PostAsync("/login",
                new StringContent(
                    JsonConvert.SerializeObject(new { email = "admin", password = "1234" }),
                    Encoding.UTF8,
                    "application/json"));

            var payload = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.token);
        }

        private async Task SeedLibrary()
        {
            var libraries = new List<Library>
            {
                new Library { Name = "Library Name 1", Location = "Location 1" },
                new Library { Name = "Library Name 2", Location = "Location 2" },
                new Library { Name = "Library Name 3", Location = "Location 3" },
                new Library { Name = "Library Name 4", Location = "Location 4" }
            };

            await context.Libraries.AddRangeAsync(libraries);
            await context.SaveChangesAsync();

            foreach (var entity in context.ChangeTracker.Entries().ToList())
                entity.State = EntityState.Detached;
        }

        private async Task SeedBook(string bookName, int libraryId)
        {
            await Client.PostAsync($"/api/libraries/{libraryId}/books",
                new StringContent(JsonConvert.SerializeObject(new { name = bookName }), Encoding.UTF8, "application/json"));
        }

        [Fact]
        public async Task TestAddBook_Ok_GetBook_NotFound()
        {
            await SeedLibrary();

            var response1 = await Client.PostAsync("/api/libraries/1/books",
                new StringContent(JsonConvert.SerializeObject(new { name = "Test book 1" }), Encoding.UTF8, "application/json"));

            response1.StatusCode.Should().BeEquivalentTo(StatusCodes.Status201Created);

            var response2 = await Client.PostAsync("/api/libraries/100/books",
                new StringContent(JsonConvert.SerializeObject(new { name = "Test book 2" }), Encoding.UTF8, "application/json"));

            response2.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task TestGetBooks_Ok_NotFound()
        {
            await SeedLibrary();

            await SeedBook("test book 1", 1);
            await SeedBook("test book 2", 1);

            var response1 = await Client.GetAsync("/api/libraries/2/books");
            response1.StatusCode.Should().BeEquivalentTo(StatusCodes.Status200OK);
            var books = JsonConvert.DeserializeObject<IEnumerable<Book>>(await response1.Content.ReadAsStringAsync())!.ToList();
            books.Count.Should().Be(0);

            var response2 = await Client.GetAsync("/api/libraries/1/books");
            response2.StatusCode.Should().BeEquivalentTo(StatusCodes.Status200OK);
            var books2 = JsonConvert.DeserializeObject<IEnumerable<Book>>(await response2.Content.ReadAsStringAsync())!.ToList();
            books2.Count.Should().Be(2);

            var response3 = await Client.GetAsync("/api/libraries/31232/books");
            response3.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task TestDeleteLibrary()
        {
            await SeedLibrary();

            var response0 = await Client.PostAsync("/api/libraries/1/books",
                new StringContent(JsonConvert.SerializeObject(new { name = "test book 1" }), Encoding.UTF8, "application/json"));
            response0.StatusCode.Should().BeEquivalentTo(StatusCodes.Status201Created);

            var response1 = await Client.DeleteAsync("/api/libraries/1");
            response1.StatusCode.Should().BeEquivalentTo(StatusCodes.Status204NoContent);

            var response2 = await Client.GetAsync("/api/libraries/1/books");
            response2.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);

            var response3 = await Client.DeleteAsync("/api/libraries/1");
            response3.StatusCode.Should().BeEquivalentTo(StatusCodes.Status404NotFound);
        }

        private record TokenResponse(string token);
    }
}
