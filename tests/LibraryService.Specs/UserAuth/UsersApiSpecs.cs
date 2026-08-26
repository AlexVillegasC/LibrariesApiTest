using System.Net.Http.Headers;

namespace LibraryService.Specs.UserAuth;

public class UsersApiSpecs : SpecFixture
{
    [Fact]
    public async Task L1_reads_self()
    {
        var (email, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.Client);
        Api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await Api.Client.GetAsync("/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<UserDto>(Json.Options);
        me!.Email.Should().Be(email);
        me.Role.Should().Be("Subscription_L1");
        me.IsActive.Should().BeTrue();
        me.SubscriptionExpirationDate.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Unauthenticated_me()
    {
        var response = await Api.CreateAnonymousClient().GetAsync("/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_lists_users()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        await AuthClient.RegisterL1Async(Api.CreateAnonymousClient(), $"listed-{Guid.NewGuid():N}@example.com");

        var response = await Api.Client.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(Json.Options);
        users.Should().Contain(u => u.Email == AuthClient.AdminEmail);
    }

    [Fact]
    public async Task Admin_gets_user_by_id()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var admin = users!.Single(u => u.Email == AuthClient.AdminEmail);

        var response = await Api.Client.GetAsync($"/users/{admin.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(Json.Options);
        user!.Email.Should().Be(AuthClient.AdminEmail);
        user.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task L1_cannot_list_users()
    {
        await AuthClient.EnsureAdminAsync(Api.Client);
        var (_, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.Client);
        var l1 = Api.CreateAnonymousClient();
        l1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await l1.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task L1_cannot_get_user_by_id()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var admin = users!.Single(u => u.Email == AuthClient.AdminEmail);
        var (_, tokens) = await AuthClient.RegisterAndLoginL1Async(Api.CreateAnonymousClient());
        var l1 = Api.CreateAnonymousClient();
        l1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await l1.GetAsync($"/users/{admin.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_role_is_absent()
    {
        await AuthClient.AuthenticateAsAdminAsync(Api.Client);
        var users = await Api.Client.GetFromJsonAsync<List<UserDto>>("/users", Json.Options);
        var admin = users!.Single(u => u.Email == AuthClient.AdminEmail);

        var response = await Api.Client.PatchAsJsonAsync(
            $"/users/{admin.Id}/role",
            new { role = "Subscription_L1" },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
