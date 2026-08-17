namespace LibraryService.Specs.BookManagement;

public class CreateBookSpecs : SpecFixture
{
    [Fact]
    public async Task Library_exists()
    {
        // Given
        var library = await Api.CreateLibraryAsync();
        await Api.AuthenticateAsAdminAsync();

        // When
        var response = await Api.Client.PostAsJsonAsync(
            $"/api/libraries/{library.Id}/books",
            new { name = "Test book 1", category = "Fiction" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = await response.Content.ReadFromJsonAsync<BookDto>(Json.Options);
        book.Should().NotBeNull();
        book!.Id.Should().BePositive();
        book.Name.Should().Be("Test book 1");
        book.Category.Should().Be("Fiction");
        book.LibraryId.Should().Be(library.Id);
    }

    [Fact]
    public async Task Library_does_not_exist()
    {
        // Given
        await Api.AuthenticateAsAdminAsync();

        // When
        var response = await Api.Client.PostAsJsonAsync(
            "/api/libraries/100/books",
            new { name = "Test book 2", category = "Fiction" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthenticated_request()
    {
        // Given
        var anonymous = Api.CreateAnonymousClient();

        // When
        var response = await anonymous.PostAsJsonAsync(
            "/api/libraries/1/books",
            new { name = "Test book 1", category = "Fiction" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
