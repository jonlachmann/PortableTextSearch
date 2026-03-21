using System.Text.Json;
using Npgsql;

namespace PortableTextSearch.Tests;

internal sealed class PostgreSqlTestConfiguration
{
    private const string LegacyConnectionEnvironmentVariable = "PORTABLE_TEXT_SEARCH_POSTGRES_CONNECTION";
    private const string AdminConnectionEnvironmentVariable = "PORTABLE_TEXT_SEARCH_POSTGRES_ADMIN_CONNECTION";
    private const string DatabaseNameEnvironmentVariable = "PORTABLE_TEXT_SEARCH_POSTGRES_DATABASE_NAME";
    private const string LocalSettingsFileName = "postgres.local.json";

    public required string AdminConnectionString { get; init; }

    public required string DatabaseNamePrefix { get; init; }

    public string CreateEphemeralDatabaseName() =>
        $"{DatabaseNamePrefix}_{Guid.NewGuid():N}";

    public string CreateTestDatabaseConnectionString(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = databaseName
        };

        return builder.ConnectionString;
    }

    public static bool IsConfigured() => TryLoad() is not null;

    public static PostgreSqlTestConfiguration? TryLoad()
    {
        var adminConnectionString = Environment.GetEnvironmentVariable(AdminConnectionEnvironmentVariable);
        var databaseName = Environment.GetEnvironmentVariable(DatabaseNameEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(adminConnectionString))
        {
            return new PostgreSqlTestConfiguration
            {
                AdminConnectionString = adminConnectionString,
                DatabaseNamePrefix = string.IsNullOrWhiteSpace(databaseName)
                    ? "portable_text_search_tests"
                    : databaseName
            };
        }

        var legacyConnectionString = Environment.GetEnvironmentVariable(LegacyConnectionEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(legacyConnectionString))
        {
            var builder = new NpgsqlConnectionStringBuilder(legacyConnectionString);
            var prefix = string.IsNullOrWhiteSpace(databaseName) ? builder.Database : databaseName;
            builder.Database = "postgres";

            return new PostgreSqlTestConfiguration
            {
                AdminConnectionString = builder.ConnectionString,
                DatabaseNamePrefix = string.IsNullOrWhiteSpace(prefix)
                    ? "portable_text_search_tests"
                    : prefix
            };
        }

        var localSettingsPath = ResolveLocalSettingsPath();
        if (localSettingsPath is null)
        {
            return null;
        }

        var json = File.ReadAllText(localSettingsPath);
        var settings = JsonSerializer.Deserialize<LocalSettingsDocument>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (settings is null || string.IsNullOrWhiteSpace(settings.AdminConnectionString))
        {
            return null;
        }

        return new PostgreSqlTestConfiguration
        {
            AdminConnectionString = settings.AdminConnectionString,
            DatabaseNamePrefix = string.IsNullOrWhiteSpace(settings.DatabaseNamePrefix)
                ? "portable_text_search_tests"
                : settings.DatabaseNamePrefix
        };
    }

    private static string? ResolveLocalSettingsPath()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, LocalSettingsFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "PortableTextSearch.Tests", LocalSettingsFileName),
            Path.Combine(Directory.GetCurrentDirectory(), LocalSettingsFileName)
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private sealed class LocalSettingsDocument
    {
        public string? AdminConnectionString { get; init; }

        public string? DatabaseNamePrefix { get; init; }
    }
}
