# PortableTextSearch

PortableTextSearch is a small EF Core 8 library that adds a provider-neutral `EF.Functions.TextContains(...)` API for substring search on PostgreSQL and SQLite.

It is designed to keep the application-facing LINQ the same while letting each provider translate to an efficient provider-specific strategy:

- PostgreSQL: `ILIKE` with `pg_trgm` indexes
- SQLite: FTS5 `MATCH` queries against a synchronized virtual table

## Install and register

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

## Querying

Use the provider-neutral query API through `EF.Functions`:

```csharp
var recipients = await context.MessageRecipients
    .Where(x => EF.Functions.TextContains(x.Email, "alice"))
    .ToListAsync();
```

For multi-field search, compose ordinary LINQ:

```csharp
var recipients = await context.MessageRecipients
    .Where(x =>
        EF.Functions.TextContains(x.Email, "alice") ||
        EF.Functions.TextContains(x.Name, "alice"))
    .ToListAsync();
```

`TextContains` is intended only for EF-translated LINQ. It throws if evaluated client-side.

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

These helpers emit SQL for:

- `CREATE EXTENSION IF NOT EXISTS pg_trgm;`
- `CREATE INDEX ... USING GIN (... gin_trgm_ops);`

### SQLite

`TextContains(field, value)` translates to an FTS5-backed `MATCH` query against a synchronized virtual table created by the SQLite migration helper.

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

The helper creates:

- the FTS5 virtual table
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

## Current scope

The current public query API is intentionally small:

- `EF.Functions.TextContains(field, value)`

That keeps the translation surface simple and portable. Multi-field search is composed with normal LINQ rather than a custom params-based API.
