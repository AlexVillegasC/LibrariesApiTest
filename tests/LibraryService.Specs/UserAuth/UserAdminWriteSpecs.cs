using System.Net.Http.Headers;

namespace LibraryService.Specs.UserAuth;

public class UserAdminWriteSpecs : SpecFixture
{
    [Fact]
    public async Task Admin_deactivates_L1()
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
        (await l1Client.GetAsync($"/api/libraries/{library.Id}/books")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var login = await Api.CreateAnonymousClient().PostAsJsonAsync(
            "/login",
            new { email, password = AuthClient.L1Password },
            Json.Options);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Last_active_Admin_cannot_be_deactivated()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var admin = users!.Single(u => u.Email == AuthClient.AdminEmail);

        var response = await Api.Client.PatchAsJsonAsync(
            $"/users/{admin.Id}/active",
            new { isActive = false },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var me = await Api.Client.GetFromJsonAsync<UserDto>("/users/me", Json.Options);
        me!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_cannot_deactivate_self()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        await Api.Client.PostAsJsonAsync(
            "/admin/register",
            new { email = "other-admin@example.com", password = AuthClient.AdminPassword },
            Json.Options);
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var self = users!.Single(u => u.Email == AuthClient.AdminEmail);

        var response = await Api.Client.PatchAsJsonAsync(
            $"/users/{self.Id}/active",
            new { isActive = false },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task L1_cannot_set_IsActive()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);
        var (_, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.Client);
        var l1 = Api.CreateAnonymousClient();
        l1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await l1.PatchAsJsonAsync(
            "/users/any-id/active",
            new { isActive = false },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Expired_L1_is_forbidden()
    {
        var library = await Api.CreateLibraryAsync();
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var (email, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.CreateAnonymousClient());
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var l1 = users!.Single(u => u.Email == email);

        var expire = await Api.Client.PatchAsJsonAsync(
            $"/users/{l1.Id}/subscription",
            new { subscriptionExpirationDate = DateTimeOffset.UtcNow.AddDays(-1) },
            Json.Options);
        expire.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var l1Client = Api.CreateAnonymousClient();
        l1Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await l1Client.GetAsync($"/api/libraries/{library.Id}/books");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_is_not_gated_by_expiry()
    {
        var library = await Api.CreateLibraryAsync();
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var admin = users!.Single(u => u.Email == AuthClient.AdminEmail);

        var expire = await Api.Client.PatchAsJsonAsync(
            $"/users/{admin.Id}/subscription",
            new { subscriptionExpirationDate = DateTimeOffset.UtcNow.AddDays(-1) },
            Json.Options);
        expire.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await Api.Client.GetAsync($"/api/libraries/{library.Id}/books");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task L1_cannot_set_expiry()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);
        var (_, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.Client);
        var l1 = Api.CreateAnonymousClient();
        l1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await l1.PatchAsJsonAsync(
            "/users/any-id/subscription",
            new { subscriptionExpirationDate = DateTimeOffset.UtcNow.AddDays(-1) },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
