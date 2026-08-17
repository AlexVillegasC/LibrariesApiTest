namespace LibraryService.Specs.LibraryManagement;

public class ListLibrariesSpecs : SpecFixture
{
    [Fact]
    public async Task Successful_list()
    {
        // Given
        await Api.CreateLibraryAsync("Library Name 1", "Location 1");

        // When
        var response = await Api.Client.GetAsync("/api/libraries");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var libraries = await response.Content.ReadFromJsonAsync<List<LibraryDto>>(Json.Options);
        libraries.Should().ContainSingle(library => library.Name == "Library Name 1" && library.Location == "Location 1");
    }
}
