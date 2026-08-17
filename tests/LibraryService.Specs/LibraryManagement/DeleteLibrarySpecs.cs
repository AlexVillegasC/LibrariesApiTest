namespace LibraryService.Specs.LibraryManagement;

public class DeleteLibrarySpecs : SpecFixture
{
    [Fact]
    public async Task Library_exists()
    {
        // Given
        var created = await Api.CreateLibraryAsync();

        // When
        var response = await Api.Client.DeleteAsync($"/api/libraries/{created.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await Api.Client.GetAsync($"/api/libraries/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Library_does_not_exist()
    {
        // When
        var response = await Api.Client.DeleteAsync("/api/libraries/31232");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Books_of_deleted_library_are_gone()
    {
        // Given
        var library = await Api.CreateLibraryAsync();
        await Api.AuthenticateAsAdminAsync();
        await Api.CreateBookAsync(library.Id, "test book 1");

        // When
        var deleteResponse = await Api.Client.DeleteAsync($"/api/libraries/{library.Id}");

        // Then
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var booksResponse = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");
        booksResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
