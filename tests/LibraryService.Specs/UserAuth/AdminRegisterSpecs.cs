using System.Net.Http.Headers;

namespace LibraryService.Specs.UserAuth;

public class AdminRegisterSpecs : SpecFixture
{
    [Fact]
    public async Task First_Admin_when_none_exist()
    {
        var response = await Api.Client.PostAsJsonAsync(
            "/admin/register",
            new { email = AuthClient.AdminEmail, password = AuthClient.AdminPassword },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await AuthClient.LoginAsync(Api.Client, AuthClient.AdminEmail, AuthClient.AdminPassword);
        Api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await Api.Client.GetFromJsonAsync<UserDto>("/users/me", Json.Options);
        me!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Anonymous_admin_register_after_an_Admin_exists()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);
        var anonymous = Api.CreateAnonymousClient();

        var response = await anonymous.PostAsJsonAsync(
            "/admin/register",
            new { email = "second@example.com", password = AuthClient.AdminPassword },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_creates_another_Admin()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);

        var response = await Api.Client.PostAsJsonAsync(
            "/admin/register",
            new { email = "second-admin@example.com", password = AuthClient.AdminPassword },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await AuthClient.LoginAsync(Api.CreateAnonymousClient(), "second-admin@example.com", AuthClient.AdminPassword);
        var client = Api.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await client.GetFromJsonAsync<UserDto>("/users/me", Json.Options);
        me!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task L1_cannot_admin_register()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);
        var (_, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.Client);
        var l1 = Api.CreateAnonymousClient();
        l1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await l1.PostAsJsonAsync(
            "/admin/register",
            new { email = "blocked-admin@example.com", password = AuthClient.AdminPassword },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
