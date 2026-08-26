using System.Net.Http.Headers;

namespace LibraryService.Specs.BookManagement;

public class ListBooksSpecs : SpecFixture
{
    [Fact]
    public async Task Library_exists_and_has_books()
    {
        // Given
        var library = await Api.CreateLibraryAsync();
        await Api.AuthenticateAsAdminAsync();
        await Api.CreateBookAsync(library.Id, "test book 1");
        await Api.CreateBookAsync(library.Id, "test book 2");

        // When
        var response = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>(Json.Options);
        books.Should().HaveCount(2);
        books.Should().Contain(book => book.Name == "test book 1" && book.LibraryId == library.Id);
        books.Should().Contain(book => book.Name == "test book 2" && book.LibraryId == library.Id);
    }

    [Fact]
    public async Task Library_exists_and_has_no_books()
    {
        // Given
        var library = await Api.CreateLibraryAsync();
        await Api.AuthenticateAsAdminAsync();

        // When
        var response = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>(Json.Options);
        books.Should().BeEmpty();
    }

    [Fact]
    public async Task Library_does_not_exist()
    {
        // Given
        await Api.AuthenticateAsAdminAsync();

        // When
        var response = await Api.Client.GetAsync("/api/libraries/31232/books");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthenticated_request()
    {
        // Given
        var anonymous = Api.CreateAnonymousClient();

        // When
        var response = await anonymous.GetAsync("/api/libraries/1/books");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Expired_L1_is_forbidden()
    {
        var library = await Api.CreateLibraryAsync();
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var (email, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.CreateAnonymousClient());
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var l1 = users!.Single(u => u.Email == email);

        var expire = await Api.Client.PatchAsJsonAsync(
            $"/users/{l1.Id}/subscription",
            new { subscriptionExpirationDate = DateTimeOffset.UtcNow.AddDays(-1) },
            Json.Options);
        expire.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var l1Client = Api.CreateAnonymousClient();
        l1Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await l1Client.GetAsync($"/api/libraries/{library.Id}/books");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
