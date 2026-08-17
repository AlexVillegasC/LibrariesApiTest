namespace LibraryService.Specs.CqrsSlices;

public class QueriesHaveNoSideEffectsSpecs : SpecFixture
{
    [Fact]
    public async Task Listing_libraries_does_not_change_data()
    {
        // Given
        await Api.CreateLibraryAsync("Library Name 1", "Location 1");

        // When
        var first = await Api.Client.GetAsync("/api/libraries");
        var second = await Api.Client.GetAsync("/api/libraries");

        // Then
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstLibraries = await first.Content.ReadFromJsonAsync<List<LibraryDto>>(Json.Options);
        var secondLibraries = await second.Content.ReadFromJsonAsync<List<LibraryDto>>(Json.Options);
        secondLibraries.Should().BeEquivalentTo(firstLibraries);
    }

    [Fact]
    public async Task Listing_books_does_not_change_data()
    {
        // Given
        var library = await Api.CreateLibraryAsync();
        await Api.AuthenticateAsAdminAsync();
        await Api.CreateBookAsync(library.Id, "test book 1");

        // When
        var first = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");
        var second = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");

        // Then
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBooks = await first.Content.ReadFromJsonAsync<List<BookDto>>(Json.Options);
        var secondBooks = await second.Content.ReadFromJsonAsync<List<BookDto>>(Json.Options);
        secondBooks.Should().BeEquivalentTo(firstBooks);
    }
}
