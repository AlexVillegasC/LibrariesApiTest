using System.Net.Http.Headers;

namespace LibraryService.Specs.UserAuth;

public class LogoutRefreshSpecs : SpecFixture
{
    [Fact]
    public async Task Logout_then_refresh_fails()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);
        var tokens = await AuthClient.LoginAsync(Api.Client, AuthClient.AdminEmail, AuthClient.AdminPassword);
        Api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var logout = await Api.Client.PostAsync("/logout", null);
        logout.IsSuccessStatusCode.Should().BeTrue();

        var anonymous = Api.CreateAnonymousClient();
        var refresh = await anonymous.PostAsJsonAsync(
            "/refresh",
            new { refreshToken = tokens.RefreshToken },
            Json.Options);

        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Valid_refresh()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);
        var tokens = await AuthClient.LoginAsync(Api.Client, AuthClient.AdminEmail, AuthClient.AdminPassword);

        var response = await Api.Client.PostAsJsonAsync(
            "/refresh",
            new { refreshToken = tokens.RefreshToken },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(Json.Options);
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }
}
