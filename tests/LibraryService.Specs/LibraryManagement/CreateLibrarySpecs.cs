namespace LibraryService.Specs.LibraryManagement;

public class CreateLibrarySpecs : SpecFixture
{
    [Fact]
    public async Task Successful_create()
    {
        // When
        var response = await Api.Client.PostAsJsonAsync(
            "/api/libraries",
            new { name = "Library Name 1", location = "Location 1" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var library = await response.Content.ReadFromJsonAsync<LibraryDto>(Json.Options);
        library.Should().NotBeNull();
        library!.Id.Should().BePositive();
        library.Name.Should().Be("Library Name 1");
        library.Location.Should().Be("Location 1");
    }
}
