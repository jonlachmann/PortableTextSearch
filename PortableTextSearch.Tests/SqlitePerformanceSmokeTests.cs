using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Query;
using Xunit.Abstractions;

namespace PortableTextSearch.Tests;

public sealed class SqlitePerformanceSmokeTests(ITestOutputHelper output)
{
    private const int SeedRowCount = 100_000;
    private const int TimedIterations = 8;
    private const int QueriesPerIteration = 25;

    [Fact]
    public async Task Fts_query_and_naive_contains_can_be_timed_via_consumer_facing_ef_queries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SqliteWorkflowIntegrationTests.SqliteWorkflowContext>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(SqliteWorkflowIntegrationTests.SqliteWorkflowContext).Assembly.FullName))
            .UsePortableTextSearch()
            .Options;

        await TextSearchPerformanceSmokeTestHarness.RunAsync(
            output,
            providerName: "SQLite",
            createContext: () => new SqliteWorkflowIntegrationTests.SqliteWorkflowContext(options),
            createEntity: (i, rowCount) => new SqliteWorkflowIntegrationTests.WorkflowRecipient
            {
                Id = i,
                MessageId = $"message-{i}",
                Type = i % 5,
                Email = i == rowCount
                    ? "needle@example.com"
                    : $"user{i:D5}@example.com",
                Name = i == rowCount
                    ? "Needle Recipient"
                    : $"Recipient {i:D5}"
            },
            emailPropertyName: nameof(SqliteWorkflowIntegrationTests.WorkflowRecipient.Email),
            seedRowCount: SeedRowCount,
            timedIterations: TimedIterations,
            queriesPerIteration: QueriesPerIteration);
    }
}
