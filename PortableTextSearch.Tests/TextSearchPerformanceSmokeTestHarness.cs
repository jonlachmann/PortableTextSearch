using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Functions;
using Xunit.Abstractions;

namespace PortableTextSearch.Tests;

internal static class TextSearchPerformanceSmokeTestHarness
{
    public static async Task RunAsync<TContext, TEntity>(
        ITestOutputHelper output,
        string providerName,
        Func<TContext> createContext,
        Func<int, int, TEntity> createEntity,
        string textSearchPropertyName,
        int seedRowCount,
        int timedIterations,
        int queriesPerIteration,
        string? naivePropertyName = null,
        Func<TContext, string, Task<PerformanceDiagnostics>>? collectDiagnosticsAsync = null)
        where TContext : DbContext
        where TEntity : class
    {
        naivePropertyName ??= textSearchPropertyName;

        await using (var setupContext = createContext())
        {
            await setupContext.Database.MigrateAsync();
            await SeedRowsAsync(setupContext, createEntity, seedRowCount);
        }

        const string term = "needle";

        if (collectDiagnosticsAsync is not null)
        {
            await using var diagnosticsContext = createContext();
            var diagnostics = await collectDiagnosticsAsync(diagnosticsContext, term);
            WriteDiagnostics(output, diagnostics);
        }

        await using (var warmupContext = createContext())
        {
            _ = await CreateFtsQuery<TEntity>(warmupContext, textSearchPropertyName, term).CountAsync();
            _ = await CreateNaiveQuery<TEntity>(warmupContext, naivePropertyName, term).CountAsync();
        }

        var ftsTimes = new List<long>();
        var naiveTimes = new List<long>();
        int? ftsCount = null;
        int? naiveCount = null;

        for (var iteration = 0; iteration < timedIterations; iteration++)
        {
            await using var ftsContext = createContext();
            var ftsStopwatch = Stopwatch.StartNew();
            for (var run = 0; run < queriesPerIteration; run++)
            {
                ftsCount = await CreateFtsQuery<TEntity>(ftsContext, textSearchPropertyName, term).CountAsync();
            }
            ftsStopwatch.Stop();
            ftsTimes.Add(ftsStopwatch.ElapsedTicks);

            await using var naiveContext = createContext();
            var naiveStopwatch = Stopwatch.StartNew();
            for (var run = 0; run < queriesPerIteration; run++)
            {
                naiveCount = await CreateNaiveQuery<TEntity>(naiveContext, naivePropertyName, term).CountAsync();
            }
            naiveStopwatch.Stop();
            naiveTimes.Add(naiveStopwatch.ElapsedTicks);
        }

        ftsCount.Should().Be(1);
        naiveCount.Should().Be(1);

        output.WriteLine($"Provider: {providerName}");
        output.WriteLine($"Seeded rows: {seedRowCount}");
        output.WriteLine($"Timed iterations: {timedIterations}");
        output.WriteLine($"Queries per iteration: {queriesPerIteration}");
        output.WriteLine($"FTS query SQL shape uses library API: EF.Functions.TextContains(EF.Property<string?>(x, \"{textSearchPropertyName}\"), \"{term}\")");
        output.WriteLine($"Naive query SQL shape uses Contains: EF.Property<string?>(x, \"{naivePropertyName}\") != null && EF.Property<string?>(x, \"{naivePropertyName}\")!.Contains(\"{term}\")");
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

    public static IQueryable<TEntity> CreateFtsQuery<TEntity>(DbContext context, string emailPropertyName, string term)
        where TEntity : class =>
        context.Set<TEntity>()
            .AsNoTracking()
            .Where(x => EF.Functions.TextContains(EF.Property<string?>(x, emailPropertyName), term));

    public static IQueryable<TEntity> CreateNaiveQuery<TEntity>(DbContext context, string emailPropertyName, string term)
        where TEntity : class =>
        context.Set<TEntity>()
            .AsNoTracking()
            .Where(x =>
                EF.Property<string?>(x, emailPropertyName) != null &&
                EF.Property<string?>(x, emailPropertyName)!.Contains(term));

    private static async Task SeedRowsAsync<TContext, TEntity>(
        TContext context,
        Func<int, int, TEntity> createEntity,
        int rowCount)
        where TContext : DbContext
        where TEntity : class
    {
        const int batchSize = 1_000;

        for (var batchStart = 1; batchStart <= rowCount; batchStart += batchSize)
        {
            var batchEnd = Math.Min(batchStart + batchSize - 1, rowCount);
            List<TEntity> batch = new(capacity: batchEnd - batchStart + 1);

            for (var i = batchStart; i <= batchEnd; i++)
            {
                batch.Add(createEntity(i, rowCount));
            }

            context.Set<TEntity>().AddRange(batch);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
    }

    private static double ToMilliseconds(double ticks) => ticks * 1000d / Stopwatch.Frequency;

    private static string ToMillisecondsString(long ticks) => $"{ToMilliseconds(ticks):0.000}";

    private static void WriteDiagnostics(ITestOutputHelper output, PerformanceDiagnostics diagnostics)
    {
        output.WriteLine("FTS generated SQL:");
        output.WriteLine(diagnostics.FtsSql);
        output.WriteLine("Naive generated SQL:");
        output.WriteLine(diagnostics.NaiveSql);

        if (diagnostics.FtsPlanLines.Count > 0)
        {
            output.WriteLine("FTS explain analyze:");
            foreach (var line in diagnostics.FtsPlanLines)
            {
                output.WriteLine(line);
            }
        }

        if (diagnostics.NaivePlanLines.Count > 0)
        {
            output.WriteLine("Naive explain analyze:");
            foreach (var line in diagnostics.NaivePlanLines)
            {
                output.WriteLine(line);
            }
        }
    }

    internal sealed class PerformanceDiagnostics
    {
        public required string FtsSql { get; init; }

        public required string NaiveSql { get; init; }

        public IReadOnlyList<string> FtsPlanLines { get; init; } = [];

        public IReadOnlyList<string> NaivePlanLines { get; init; } = [];
    }
}
