using System.Net.Http.Headers;

namespace LibraryService.Specs.UserAuth;

public class TokenAuthorizesBooksSpecs : SpecFixture
{
    [Fact]
    public async Task Issued_token_is_accepted()
    {
        // Given
        var library = await Api.CreateLibraryAsync();
        var token = await AuthClient.LoginAsAdminAsync(Api.Client);
        Api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // When
        var response = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_or_invalid_token_is_rejected()
    {
        // Given
        var anonymous = Api.CreateAnonymousClient();
        var invalid = Api.CreateAnonymousClient();
        invalid.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        // When
        var missingResponse = await anonymous.GetAsync("/api/libraries/1/books");
        var invalidResponse = await invalid.GetAsync("/api/libraries/1/books");

        // Then
        missingResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
