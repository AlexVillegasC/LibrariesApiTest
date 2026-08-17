namespace LibraryService.Specs.UserAuth;

public class LoginSpecs : SpecFixture
{
    [Fact]
    public async Task Valid_credentials()
    {
        // When
        var response = await Api.Client.PostAsJsonAsync(
            "/login",
            new { email = "admin", password = "1234" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json.Options);
        body!.token.Should().NotBeNullOrEmpty();
        body.token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public async Task Invalid_credentials()
    {
        // When
        var response = await Api.Client.PostAsJsonAsync(
            "/login",
            new { email = "admin", password = "wrong" },
            Json.Options);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
