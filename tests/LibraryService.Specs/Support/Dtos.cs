using System.Text.Json;

namespace LibraryService.Specs.Support;

public sealed record LibraryDto(int Id, string Name, string Location);

public sealed record BookDto(int Id, string Name, string Category, int LibraryId);

public sealed record AccessTokenResponse(string AccessToken, string RefreshToken, long ExpiresIn, string TokenType);

public sealed record UserDto(
    string Id,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset SubscriptionExpirationDate);

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
