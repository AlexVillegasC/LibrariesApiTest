using System.Text.Json;

namespace LibraryService.Specs.Support;

public sealed record LibraryDto(int Id, string Name, string Location);

public sealed record BookDto(int Id, string Name, string Category, int LibraryId);

public sealed record TokenResponse(string token);

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
