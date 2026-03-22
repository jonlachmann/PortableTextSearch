# PortableTextSearch

PortableTextSearch is a small EF Core library that adds provider-neutral `EF.Functions.TextContains(...)` and `EF.Functions.TextContainsAny(...)` APIs for substring search on PostgreSQL and SQLite.

The repository now ships separate package lines for EF Core 8, 9, and 10. The public API stays the same across all three lines, but the package version must match the EF Core major version used by the consuming application.

It is designed to keep the application-facing LINQ the same while letting each provider translate to an efficient provider-specific strategy:

- PostgreSQL: `ILIKE` with `pg_trgm` indexes
- SQLite: FTS5 `MATCH` queries against a synchronized virtual table

## Install and register

PortableTextSearch currently supports:

- package `8.x`: .NET 8, EF Core 8.x, Npgsql EF Core 8.x, SQLite EF Core 8.x
- package `9.x`: .NET 8, EF Core 9.x, Npgsql EF Core 9.x, SQLite EF Core 9.x
- package `10.x`: .NET 10, EF Core 10.x, Npgsql EF Core 10.x, SQLite EF Core 10.x

The package id is the same for all lines:

- `PortableTextSearch.EntityFrameworkCore`

Choose the package version that matches your EF Core major version.

Register PortableTextSearch on the same `DbContextOptionsBuilder` where the database provider is configured:

```csharp
optionsBuilder
    .UseNpgsql(connectionString)
    .UsePortableTextSearch();
```

The same registration works for SQLite:

```csharp
optionsBuilder
    .UseSqlite("Data Source=portable-text-search.db")
    .UsePortableTextSearch();
```

Example installation:

```bash
dotnet add package PortableTextSearch.EntityFrameworkCore --version 9.0.9-alpha.1
```

## Consuming app checklist

1. Install the package version matching your EF Core major version.
2. Register the provider and PortableTextSearch on the same `DbContextOptionsBuilder`.
3. Mark searchable string properties with `HasTextSearch(...)`.
4. Query with `EF.Functions.TextContains(...)` or `EF.Functions.TextContainsAny(...)`.
5. If you want helper-based scaffolded migrations, add the design-time shim in the startup project.
6. Scaffold and apply migrations as usual with `dotnet ef migrations add ...` and `dotnet ef database update`.

Minimal setup:

```csharp
optionsBuilder
    .UseSqlite(connectionString)
    .UsePortableTextSearch();
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MessageRecipient>()
        .HasTextSearch(x => x.Email)
        .HasTextSearch(x => x.Name);
}
```

If you want scaffolded migrations to call the PortableTextSearch helper methods instead of raw `migrationBuilder.Sql(...)`, add a small EF design-time shim to the startup project where you run `dotnet ef`:

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using PortableTextSearch.Design;

internal sealed class DesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        new PortableTextSearchDesignTimeServices().ConfigureDesignTimeServices(services);
    }
}
```

## Configure searchable fields

Mark mapped string properties with `HasTextSearch(...)` in model configuration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MessageRecipient>()
        .HasTextSearch(x => x.Email)
        .HasTextSearch(x => x.Name);
}
```

The configuration is stored as EF Core model metadata. The expression must be a simple mapped string property access such as `x => x.Email`.

When `HasTextSearch(...)` is added or removed, EF Core migrations now pick that up automatically. PortableTextSearch contributes provider-specific SQL operations during model diffing so a newly scaffolded migration is not empty.

## Querying

Use the provider-neutral query API through `EF.Functions`:

```csharp
var recipients = await context.MessageRecipients
    .Where(x => EF.Functions.TextContains(x.Email, "alice"))
    .ToListAsync();
```

For multi-field search, you can either compose ordinary LINQ:

```csharp
var recipients = await context.MessageRecipients
    .Where(x =>
        EF.Functions.TextContains(x.Email, "alice") ||
        EF.Functions.TextContains(x.Name, "alice"))
    .ToListAsync();
```

Or use `TextContainsAny(...)` for 2-32 fields:

```csharp
var recipients = await context.MessageRecipients
    .Where(x => EF.Functions.TextContainsAny("alice", x.Email, x.Name))
    .ToListAsync();
```

```csharp
var recipients = await context.MessageRecipients
    .Where(x => EF.Functions.TextContainsAny(
        "alice",
        x.Email,
        x.Name,
        x.AddressLine1,
        x.AddressLine2,
        x.CompanyName,
        x.Notes))
    .ToListAsync();
```

`TextContains` and `TextContainsAny` are intended only for EF-translated LINQ. They throw if evaluated client-side.

## Provider behavior

### PostgreSQL

`TextContains(field, value)` translates to:

```sql
field ILIKE '%' || value || '%'
```

This is intended to pair with `pg_trgm` GIN indexes.

Migration helpers:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.EnsurePostgresTrigramExtension();

    migrationBuilder.CreatePostgresTextSearchIndex(
        table: "MessageRecipients",
        column: "Email");

    migrationBuilder.CreatePostgresTextSearchIndex(
        table: "MessageRecipients",
        column: "Name");
}
```

When a migration is scaffolded from `HasTextSearch(...)`, PortableTextSearch can emit the equivalent helper-method calls automatically when the design-time shim above is present. The helpers below remain available if you prefer to write or customize the migration by hand.

These helpers emit SQL for:

- `CREATE EXTENSION IF NOT EXISTS pg_trgm;`
- `CREATE INDEX ... USING GIN (... gin_trgm_ops);`

### SQLite

`TextContains(field, value)` translates to an FTS5-backed `MATCH` query against a synchronized virtual table created by the SQLite migration helper.
The SQLite implementation supports integer and Guid keys by storing the real entity key in an unindexed FTS column rather than relying on the FTS table's internal `rowid`.

Migration helpers:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "MessageRecipients",
        columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false),
            Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
            Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
        },
        constraints: table => table.PrimaryKey("PK_MessageRecipients", x => x.Id));

    migrationBuilder.CreateSqliteTextSearchIndex(
        table: "MessageRecipients",
        columns: ["Email", "Name"],
        contentRowIdColumn: "Id");
}
```

When a migration is scaffolded from `HasTextSearch(...)`, PortableTextSearch can emit the equivalent helper-method calls automatically when the design-time shim above is present. The helper remains available if you want to author the migration explicitly.

The helper creates:

- the FTS5 virtual table
- an unindexed key column used to link FTS matches back to the base table
- a seed statement for existing rows
- insert, update, and delete synchronization triggers

Current SQLite caveat:

- query translation assumes the default virtual table naming convention used by `CreateSqliteTextSearchIndex(...)`
- custom SQLite virtual table names are supported by the migration helper, but query translation is not yet model-configurable for custom names

## Tests

The solution includes:

- model-configuration tests
- SQL translation tests for PostgreSQL and SQLite
- migration helper tests
- end-to-end SQLite workflow tests against a real in-memory SQLite database
- end-to-end PostgreSQL workflow and performance tests against a real local PostgreSQL server
- performance smoke tests comparing `TextContains(...)` with a naive `Contains(...)` query

This repo now carries parallel test projects for all supported EF Core majors:

- `PortableTextSearch.Tests` validates the EF Core 8 package line
- `PortableTextSearch.Tests.EF9` validates the EF Core 9 package line
- `PortableTextSearch.Tests.EF10` validates the EF Core 10 package line

### PostgreSQL local test setup

PostgreSQL integration and performance tests are opt-in. They look for either:

- `PortableTextSearch.Tests/postgres.local.json`
- environment overrides

Start from `PortableTextSearch.Tests/postgres.local.json.example`:

```json
{
  "AdminConnectionString": "Host=localhost;Database=postgres;Username=test;Password=test",
  "DatabaseNamePrefix": "portable_text_search_tests"
}
```

The configured account must be able to:

- connect to the maintenance database
- create and drop databases
- create the `pg_trgm` extension inside the temporary test database

Each PostgreSQL integration test run creates a fresh temporary database, applies migrations, runs the test workflow, and drops the database during teardown.

Environment overrides are also supported:

- `PORTABLE_TEXT_SEARCH_POSTGRES_ADMIN_CONNECTION`
- `PORTABLE_TEXT_SEARCH_POSTGRES_DATABASE_NAME`

## EF version differences

The package lines share the same public API and the same behavior goals. The main differences between EF Core 8, 9, and 10 support are internal:

- EF8 uses older internal constructor and SQL-processor signatures in the SQLite query pipeline.
- EF9 and EF10 use newer `SelectExpression` and relational SQL-processor shapes.
- EF10 requires a different relational parameter-based SQL processor entry point than EF8/EF9.

That version-specific code is isolated to the provider-integration layer. The public APIs, migration helpers, and test coverage stay aligned across all three package lines.
