using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace LibraryService.Specs.Support;

public static class AuthClient
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin1a";
    public const string L1Password = "Reader1";

    public static async Task EnsureAdminAsync(HttpClient client)
    {
        await client.PostAsJsonAsync(
            "/admin/register",
            new { email = AdminEmail, password = AdminPassword },
            Json.Options);
    }

    public static async Task<HttpResponseMessage> RegisterL1Async(HttpClient client, string email, string password = L1Password)
    {
        return await client.PostAsJsonAsync(
            "/register",
            new { email, password },
            Json.Options);
    }

    public static async Task<AccessTokenResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/login", new { email, password }, Json.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(Json.Options);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        return body;
    }

    public static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        await EnsureAdminAsync(client);
        var tokens = await LoginAsync(client, AdminEmail, AdminPassword);
        return tokens.AccessToken;
    }

    public static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<(string Email, AccessTokenResponse Tokens)> RegisterAndLoginL1Async(
        HttpClient client,
        string? email = null)
    {
        email ??= $"l1-{Guid.NewGuid():N}@example.com";
        var register = await RegisterL1Async(client, email);
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await LoginAsync(client, email, L1Password);
        return (email, tokens);
    }
}
