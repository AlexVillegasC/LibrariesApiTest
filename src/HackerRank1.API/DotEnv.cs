namespace LibraryService.Api;

internal static class DotEnv
{
    public static void Load()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        MergeIfFound(values, ".env");
        if (!string.IsNullOrWhiteSpace(environment))
            MergeIfFound(values, $".env.{environment.ToLowerInvariant()}");

        foreach (var (key, value) in values)
        {
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }

        if (Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") is null)
        {
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString))
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
        }
    }

    private static void MergeIfFound(IDictionary<string, string> values, string fileName)
    {
        var path = SearchDirectories()
            .Select(directory => Path.Combine(directory, fileName))
            .FirstOrDefault(File.Exists);

        if (path is null)
            return;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = trimmed[..separator].Trim();
            var value = Unquote(trimmed[(separator + 1)..].Trim());
            if (key.Length == 0)
                continue;

            values[key] = value;
        }
    }

    private static IEnumerable<string> SearchDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = start; !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
            {
                if (seen.Add(directory))
                    yield return directory;
            }
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            return value[1..^1];

        return value;
    }
}
