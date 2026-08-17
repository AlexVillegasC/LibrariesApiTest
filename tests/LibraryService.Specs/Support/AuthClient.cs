using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace LibraryService.Specs.Support;

public static class AuthClient
{
    public static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/login", new { email = "admin", password = "1234" }, Json.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json.Options);
        body!.token.Should().NotBeNullOrEmpty();
        return body.token;
    }

    public static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
