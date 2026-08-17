namespace LibraryService.Specs.LibraryManagement;

public class GetLibrarySpecs : SpecFixture
{
    [Fact]
    public async Task Library_exists()
    {
        // Given
        var created = await Api.CreateLibraryAsync("Library Name 1", "Location 1");

        // When
        var response = await Api.Client.GetAsync($"/api/libraries/{created.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var library = await response.Content.ReadFromJsonAsync<LibraryDto>(Json.Options);
        library.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Library_does_not_exist()
    {
        // When
        var response = await Api.Client.GetAsync("/api/libraries/31232");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
