using System.Net.Http.Headers;

namespace LibraryService.Specs.UserAuth;

public class TokenAuthorizesBooksSpecs : SpecFixture
{
    [Fact]
    public async Task Issued_token_is_accepted()
    {
        var library = await Api.CreateLibraryAsync();
        var token = await AuthClient.LoginAsAdminAsync(Api.Client);
        Api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_or_invalid_token_is_rejected()
    {
        var anonymous = Api.CreateAnonymousClient();
        var invalid = Api.CreateAnonymousClient();
        invalid.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var missingResponse = await anonymous.GetAsync("/api/libraries/1/books");
        var invalidResponse = await invalid.GetAsync("/api/libraries/1/books");

        missingResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inactive_token_is_rejected()
    {
        var library = await Api.CreateLibraryAsync();
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var (email, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.CreateAnonymousClient());
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var l1 = users!.Single(u => u.Email == email);

        var deactivate = await Api.Client.PatchAsJsonAsync(
            $"/users/{l1.Id}/active",
            new { isActive = false },
            Json.Options);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var l1Client = Api.CreateAnonymousClient();
        l1Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await l1Client.GetAsync($"/api/libraries/{library.Id}/books");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
