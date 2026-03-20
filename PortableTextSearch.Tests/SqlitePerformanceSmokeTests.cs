using System.Diagnostics;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Functions;
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

        await using (var context = new SqliteWorkflowIntegrationTests.SqliteWorkflowContext(options))
        {
            await context.Database.MigrateAsync();
            await SeedRowsAsync(context, rowCount: SeedRowCount);
        }

        const string term = "needle";

        await using var warmupContext = new SqliteWorkflowIntegrationTests.SqliteWorkflowContext(options);
        _ = await warmupContext.Recipients
            .AsNoTracking()
            .Where(x => EF.Functions.TextContains(x.Email, term))
            .CountAsync();
        _ = await warmupContext.Recipients
            .AsNoTracking()
            .Where(x => x.Email != null && x.Email.Contains(term))
            .CountAsync();

        var ftsTimes = new List<long>();
        var naiveTimes = new List<long>();
        int? ftsCount = null;
        int? naiveCount = null;

        for (var iteration = 0; iteration < TimedIterations; iteration++)
        {
            await using var ftsContext = new SqliteWorkflowIntegrationTests.SqliteWorkflowContext(options);
            var ftsStopwatch = Stopwatch.StartNew();
            for (var run = 0; run < QueriesPerIteration; run++)
            {
                ftsCount = await ftsContext.Recipients
                    .AsNoTracking()
                    .Where(x => EF.Functions.TextContains(x.Email, term))
                    .CountAsync();
            }
            ftsStopwatch.Stop();
            ftsTimes.Add(ftsStopwatch.ElapsedTicks);

            await using var naiveContext = new SqliteWorkflowIntegrationTests.SqliteWorkflowContext(options);
            var naiveStopwatch = Stopwatch.StartNew();
            for (var run = 0; run < QueriesPerIteration; run++)
            {
                naiveCount = await naiveContext.Recipients
                    .AsNoTracking()
                    .Where(x => x.Email != null && x.Email.Contains(term))
                    .CountAsync();
            }
            naiveStopwatch.Stop();
            naiveTimes.Add(naiveStopwatch.ElapsedTicks);
        }

        ftsCount.Should().Be(1);
        naiveCount.Should().Be(1);

        output.WriteLine($"Seeded rows: {SeedRowCount}");
        output.WriteLine($"Timed iterations: {TimedIterations}");
        output.WriteLine($"Queries per iteration: {QueriesPerIteration}");
        output.WriteLine($"FTS query SQL shape uses library API: EF.Functions.TextContains(x.Email, \"{term}\")");
        output.WriteLine($"Naive query SQL shape uses Contains: x.Email != null && x.Email.Contains(\"{term}\")");
        output.WriteLine($"Stopwatch frequency: {Stopwatch.Frequency} ticks/second");
        output.WriteLine($"FTS timings ticks: {string.Join(", ", ftsTimes)}");
        output.WriteLine($"Naive timings ticks: {string.Join(", ", naiveTimes)}");
        output.WriteLine($"FTS timings ms: {string.Join(", ", ftsTimes.Select(ToMillisecondsString))}");
        output.WriteLine($"Naive timings ms: {string.Join(", ", naiveTimes.Select(ToMillisecondsString))}");
        output.WriteLine($"FTS avg ms: {ToMilliseconds(ftsTimes.Average()):0.000}");
        output.WriteLine($"Naive avg ms: {ToMilliseconds(naiveTimes.Average()):0.000}");
        output.WriteLine($"FTS min ms: {ToMilliseconds(ftsTimes.Min()):0.000}");
        output.WriteLine($"Naive min ms: {ToMilliseconds(naiveTimes.Min()):0.000}");
        output.WriteLine($"FTS/Naive avg ratio: {ftsTimes.Average() / naiveTimes.Average():0.000}");
    }

    private static async Task SeedRowsAsync(SqliteWorkflowIntegrationTests.SqliteWorkflowContext context, int rowCount)
    {
        const int batchSize = 1_000;

        for (var batchStart = 1; batchStart <= rowCount; batchStart += batchSize)
        {
            var batch = new List<SqliteWorkflowIntegrationTests.WorkflowRecipient>(capacity: batchSize);
            var batchEnd = Math.Min(batchStart + batchSize - 1, rowCount);

            for (var i = batchStart; i <= batchEnd; i++)
            {
                batch.Add(new SqliteWorkflowIntegrationTests.WorkflowRecipient
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
                });
            }

            context.Recipients.AddRange(batch);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
    }

    private static double ToMilliseconds(double ticks) => ticks * 1000d / Stopwatch.Frequency;

    private static string ToMillisecondsString(long ticks) => $"{ToMilliseconds(ticks):0.000}";
}
