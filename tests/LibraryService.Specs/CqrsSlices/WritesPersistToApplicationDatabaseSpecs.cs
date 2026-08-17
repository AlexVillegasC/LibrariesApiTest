namespace LibraryService.Specs.CqrsSlices;

public class WritesPersistToApplicationDatabaseSpecs : SpecFixture
{
    [Fact]
    public async Task Created_library_is_readable()
    {
        // When
        var created = await Api.CreateLibraryAsync("Library Name 1", "Location 1");
        var response = await Api.Client.GetAsync("/api/libraries");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var libraries = await response.Content.ReadFromJsonAsync<List<LibraryDto>>(Json.Options);
        libraries.Should().ContainEquivalentOf(created);
    }

    [Fact]
    public async Task Created_book_is_readable()
    {
        // Given
        var library = await Api.CreateLibraryAsync();
        await Api.AuthenticateAsAdminAsync();

        // When
        var created = await Api.CreateBookAsync(library.Id, "Test book 1", "Fiction");
        var response = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>(Json.Options);
        books.Should().ContainEquivalentOf(created);
    }
}
