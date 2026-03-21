using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortableTextSearch.Functions;
using PortableTextSearch.Query;

namespace PortableTextSearch.Tests;

public sealed class PostgreSqlWorkflowIntegrationTests
{
    [RequiresPostgresFact]
    public async Task Migration_and_query_workflow_runs_against_postgresql()
    {
        var configuration = PostgreSqlTestConfiguration.TryLoad()
            ?? throw new InvalidOperationException("PostgreSQL test configuration is required.");
        await using var databaseScope = await PostgreSqlTestDatabaseScope.CreateAsync(configuration);
        var connectionString = databaseScope.DatabaseConnectionString;

        var options = new DbContextOptionsBuilder<PostgreSqlWorkflowContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql
                    .MigrationsAssembly(typeof(PostgreSqlWorkflowContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", PostgreSqlWorkflowFixture.SchemaName))
            .UsePortableTextSearch()
            .Options;

        await using (var setupContext = new PostgreSqlWorkflowContext(options))
        {
            await setupContext.Database.MigrateAsync();

            setupContext.Recipients.Add(new PostgreSqlWorkflowRecipient
            {
                Id = 1,
                MessageId = "message-1",
                Type = 7,
                Email = "alice@example.com",
                UnindexedEmail = "alice@example.com",
                Name = "Alice Johnson"
            });
            setupContext.Recipients.Add(new PostgreSqlWorkflowRecipient
            {
                Id = 2,
                MessageId = "message-2",
                Type = 7,
                Email = "bob@example.com",
                UnindexedEmail = "bob@example.com",
                Name = "Alice Cooper"
            });

            await setupContext.SaveChangesAsync();
        }

        await using (var queryContext = new PostgreSqlWorkflowContext(options))
        {
            var querySql = queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToQueryString();

            querySql.Should().Contain("ILIKE");
            querySql.Should().Contain("\"Email\"");

            var emailMatches = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToListAsync();

            emailMatches.Should().ContainSingle()
                .Which.Id.Should().Be(1);

            var nameMatches = await queryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Name, "alice"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            nameMatches.Select(x => x.Id).Should().Equal(1, 2);
        }

        await using (var updateContext = new PostgreSqlWorkflowContext(options))
        {
            var recipient = (await updateContext.Recipients
                .Where(x => x.Id == 1)
                .ToListAsync())
                .Single();
            recipient.Email = "carol@example.com";
            recipient.UnindexedEmail = "carol@example.com";
            recipient.Name = "Carol Stone";
            await updateContext.SaveChangesAsync();
        }

        await using (var updatedQueryContext = new PostgreSqlWorkflowContext(options))
        {
            var emailAliceMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "alice"))
                .ToListAsync();

            emailAliceMatches.Should().BeEmpty();

            var emailCarolMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "carol"))
                .ToListAsync();

            emailCarolMatches.Should().ContainSingle()
                .Which.Id.Should().Be(1);

            var nameAliceMatches = await updatedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Name, "alice"))
                .ToListAsync();

            nameAliceMatches.Should().ContainSingle()
                .Which.Id.Should().Be(2);
        }

        await using (var deleteContext = new PostgreSqlWorkflowContext(options))
        {
            var recipient = (await deleteContext.Recipients
                .Where(x => x.Id == 1)
                .ToListAsync())
                .Single();
            deleteContext.Remove(recipient);
            await deleteContext.SaveChangesAsync();
        }

        await using (var deletedQueryContext = new PostgreSqlWorkflowContext(options))
        {
            var emailCarolMatches = await deletedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "carol"))
                .ToListAsync();

            emailCarolMatches.Should().BeEmpty();

            var remainingMatches = await deletedQueryContext.Recipients
                .Where(x => EF.Functions.TextContains(x.Email, "bob"))
                .ToListAsync();

            remainingMatches.Should().ContainSingle()
                .Which.Id.Should().Be(2);
        }
    }
}
