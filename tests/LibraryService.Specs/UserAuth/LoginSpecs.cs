namespace LibraryService.Specs.UserAuth;

public class LoginSpecs : SpecFixture
{
    [Fact]
    public async Task Valid_credentials()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);

        var response = await Api.Client.PostAsJsonAsync(
            "/login",
            new { email = AuthClient.AdminEmail, password = AuthClient.AdminPassword },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(Json.Options);
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Invalid_credentials()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);

        var response = await Api.Client.PostAsJsonAsync(
            "/login",
            new { email = AuthClient.AdminEmail, password = "wrong1" },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inactive_user_cannot_login()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var (email, _) = await AuthClient.RegisterAndLoginL1Async(Api.CreateAnonymousClient());
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var l1 = users!.Single(u => u.Email == email);

        var deactivate = await Api.Client.PatchAsJsonAsync(
            $"/users/{l1.Id}/active",
            new { isActive = false },
            Json.Options);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonymous = Api.CreateAnonymousClient();
        var response = await anonymous.PostAsJsonAsync(
            "/login",
            new { email, password = AuthClient.L1Password },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
