using Npgsql;

namespace PortableTextSearch.Tests;

internal sealed class PostgreSqlTestDatabaseScope : IAsyncDisposable
{
    public required string DatabaseName { get; init; }

    public required string DatabaseConnectionString { get; init; }

    public required string AdminConnectionString { get; init; }

    public static async Task<PostgreSqlTestDatabaseScope> CreateAsync(PostgreSqlTestConfiguration configuration)
    {
        var databaseName = configuration.CreateEphemeralDatabaseName();

        await CreateDatabaseAsync(configuration.AdminConnectionString, databaseName);

        return new PostgreSqlTestDatabaseScope
        {
            DatabaseName = databaseName,
            DatabaseConnectionString = configuration.CreateTestDatabaseConnectionString(databaseName),
            AdminConnectionString = configuration.AdminConnectionString
        };
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await DropDatabaseAsync(AdminConnectionString, DatabaseName);
    }

    private static async Task CreateDatabaseAsync(string adminConnectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)};";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string adminConnectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        var escapedDatabaseName = databaseName.Replace("'", "''", StringComparison.Ordinal);

        await using (var terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{escapedDatabaseName}'
                  AND pid <> pg_backend_pid();
                """;
            try
            {
                await terminateCommand.ExecuteNonQueryAsync();
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
            {
                // Some local test accounts can create/drop the ephemeral database but are not allowed
                // to terminate other backends via pg_terminate_backend. Clearing pools first is usually
                // enough for our own sessions, so fall through and attempt the drop anyway.
            }
        }

        await using (var dropCommand = connection.CreateCommand())
        {
            dropCommand.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)};";
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
