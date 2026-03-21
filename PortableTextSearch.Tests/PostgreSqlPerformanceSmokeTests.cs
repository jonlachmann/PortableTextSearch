using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortableTextSearch.Query;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace PortableTextSearch.Tests;

public sealed class PostgreSqlPerformanceSmokeTests(ITestOutputHelper output)
{
    private const int SeedRowCount = 100_000;
    private const int TimedIterations = 8;
    private const int QueriesPerIteration = 25;

    [RequiresPostgresFact]
    public async Task Trigram_query_and_naive_contains_can_be_timed_via_consumer_facing_ef_queries()
    {
        var configuration = PostgreSqlTestConfiguration.TryLoad()
            ?? throw new InvalidOperationException("PostgreSQL test configuration is required.");
        await using var databaseScope = await PostgreSqlTestDatabaseScope.CreateAsync(configuration);

        var options = new DbContextOptionsBuilder<PostgreSqlWorkflowIntegrationTests.PostgreSqlWorkflowContext>()
            .UseNpgsql(
                databaseScope.DatabaseConnectionString,
                npgsql => npgsql
                    .MigrationsAssembly(typeof(PostgreSqlWorkflowIntegrationTests.PostgreSqlWorkflowContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "pts_pgsql_workflow"))
            .UsePortableTextSearch()
            .Options;

        await TextSearchPerformanceSmokeTestHarness.RunAsync(
            output,
            providerName: "PostgreSQL",
            createContext: () => new PostgreSqlWorkflowIntegrationTests.PostgreSqlWorkflowContext(options),
            createEntity: (i, rowCount) => new PostgreSqlWorkflowIntegrationTests.WorkflowRecipient
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
            emailPropertyName: nameof(PostgreSqlWorkflowIntegrationTests.WorkflowRecipient.Email),
            seedRowCount: SeedRowCount,
            timedIterations: TimedIterations,
            queriesPerIteration: QueriesPerIteration,
            collectDiagnosticsAsync: (context, term) => CollectDiagnosticsAsync(context, term));
    }

    private static async Task<TextSearchPerformanceSmokeTestHarness.PerformanceDiagnostics> CollectDiagnosticsAsync(
        PostgreSqlWorkflowIntegrationTests.PostgreSqlWorkflowContext context,
        string term)
    {
        var emailPropertyName = nameof(PostgreSqlWorkflowIntegrationTests.WorkflowRecipient.Email);
        var ftsQuery = TextSearchPerformanceSmokeTestHarness
            .CreateFtsQuery<PostgreSqlWorkflowIntegrationTests.WorkflowRecipient>(context, emailPropertyName, term);
        var naiveQuery = TextSearchPerformanceSmokeTestHarness
            .CreateNaiveQuery<PostgreSqlWorkflowIntegrationTests.WorkflowRecipient>(context, emailPropertyName, term);

        var ftsSql = ftsQuery.ToQueryString();
        var naiveSql = naiveQuery.ToQueryString();

        return new TextSearchPerformanceSmokeTestHarness.PerformanceDiagnostics
        {
            FtsSql = ftsSql,
            NaiveSql = naiveSql,
            FtsPlanLines = await ExplainAnalyzeAsync(context, ftsSql),
            NaivePlanLines = await ExplainAnalyzeAsync(context, naiveSql)
        };
    }

    private static async Task<IReadOnlyList<string>> ExplainAnalyzeAsync(DbContext context, string sql)
    {
        var executableSql = InlineToQueryStringParameters(sql);
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"EXPLAIN (ANALYZE, BUFFERS) {executableSql}";

        List<string> lines = [];
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return lines;
    }

    private static string InlineToQueryStringParameters(string sql)
    {
        Dictionary<string, string> parameters = [];
        List<string> sqlLines = [];

        foreach (var line in sql.Split(Environment.NewLine))
        {
            if (TryParseParameterDeclaration(line, out var name, out var literalValue))
            {
                parameters.Add(name, literalValue);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                sqlLines.Add(line);
            }
        }

        var executableSql = string.Join(Environment.NewLine, sqlLines);
        foreach (var parameter in parameters)
        {
            executableSql = executableSql.Replace(parameter.Key, parameter.Value, StringComparison.Ordinal);
        }

        return executableSql;
    }

    private static bool TryParseParameterDeclaration(string line, out string name, out string literalValue)
    {
        var match = Regex.Match(
            line,
            @"^--\s+(?<name>@[A-Za-z0-9_]+)='(?<value>(?:''|[^'])*)'(?:\s+\([^)]+\))?$",
            RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            name = string.Empty;
            literalValue = string.Empty;
            return false;
        }

        name = match.Groups["name"].Value;
        literalValue = $"'{match.Groups["value"].Value}'";
        return true;
    }
}
