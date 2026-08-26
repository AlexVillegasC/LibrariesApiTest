using System.Net.Http.Headers;

namespace LibraryService.Specs.UserAuth;

public class RegisterSpecs : SpecFixture
{
    [Fact]
    public async Task Successful_L1_register()
    {
        var email = $"l1-{Guid.NewGuid():N}@example.com";
        var before = DateTimeOffset.UtcNow;

        var response = await AuthClient.RegisterL1Async(Api.Client, email);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await AuthClient.LoginAsync(Api.Client, email, AuthClient.L1Password);
        Api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await Api.Client.GetFromJsonAsync<UserDto>("/users/me", Json.Options);

        me!.Role.Should().Be("Subscription_L1");
        me.IsActive.Should().BeTrue();
        me.SubscriptionExpirationDate.Should().BeCloseTo(before.AddYears(1), TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task Duplicate_email()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        (await AuthClient.RegisterL1Async(Api.Client, email)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await AuthClient.RegisterL1Async(Api.Client, email);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Password_below_policy()
    {
        var email = $"weak-{Guid.NewGuid():N}@example.com";

        var tooShort = await Api.Client.PostAsJsonAsync("/register", new { email, password = "Ab1" }, Json.Options);
        var noDigit = await Api.Client.PostAsJsonAsync("/register", new { email, password = "abcdef" }, Json.Options);
        var noLetter = await Api.Client.PostAsJsonAsync("/register", new { email, password = "123456" }, Json.Options);

        tooShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        noDigit.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        noLetter.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Letter_and_digit_of_length_6_is_accepted()
    {
        var email = $"ok-{Guid.NewGuid():N}@example.com";

        var response = await Api.Client.PostAsJsonAsync(
            "/register",
            new { email, password = "abcde1" },
            Json.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
