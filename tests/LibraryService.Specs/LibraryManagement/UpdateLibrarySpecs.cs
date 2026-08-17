namespace LibraryService.Specs.LibraryManagement;

public class UpdateLibrarySpecs : SpecFixture
{
    [Fact]
    public async Task Library_exists()
    {
        // Given
        var created = await Api.CreateLibraryAsync("Library Name 1", "Location 1");

        // When
        var response = await Api.Client.PutAsJsonAsync(
            $"/api/libraries/{created.Id}",
            new { name = "Updated Name", location = "Updated Location" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await Api.Client.GetAsync($"/api/libraries/{created.Id}");
        var library = await getResponse.Content.ReadFromJsonAsync<LibraryDto>(Json.Options);
        library!.Name.Should().Be("Updated Name");
        library.Location.Should().Be("Updated Location");
    }

    [Fact]
    public async Task Library_does_not_exist()
    {
        // When
        var response = await Api.Client.PutAsJsonAsync(
            "/api/libraries/31232",
            new { name = "Updated Name", location = "Updated Location" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
