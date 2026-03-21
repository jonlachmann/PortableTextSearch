# PortableTextSearch

PortableTextSearch is a small EF Core 8 extension library that adds a provider-neutral `TextContains` LINQ API for portable substring search across PostgreSQL and SQLite.

## Current provider behavior

- PostgreSQL: translated to `ILIKE '%' || value || '%'`, designed to pair with `pg_trgm` indexes.
- SQLite: translated to FTS5-backed `MATCH` queries against a synchronized virtual table, with the row filter correlated back to the base table by primary key.

## Configure searchable fields

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MessageRecipient>()
        .HasTextSearch(x => x.Email)
        .HasTextSearch(x => x.Name);
}
```

The configuration stores searchable-field metadata on the EF model and validates that each expression targets a mapped string property.

## Querying

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

Register the extension when configuring the context:

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

## PostgreSQL migration helpers

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.EnsurePostgresTrigramExtension();
    migrationBuilder.CreatePostgresTextSearchIndex(
        table: "MessageRecipients",
        column: "Email");
}
```

The helper emits SQL for:

- `CREATE EXTENSION IF NOT EXISTS pg_trgm;`
- `CREATE INDEX ... USING GIN (... gin_trgm_ops);`

## PostgreSQL integration test

The test suite includes a real PostgreSQL migration-and-query workflow test. It is opt-in so the default suite remains portable, but it no longer depends on run-time environment variables to work in IDE test runners.

Copy [postgres.local.json.example](/Users/jonlachmann/Dev/csharp/EfTextContains/PortableTextSearch.Tests/postgres.local.json.example) to `PortableTextSearch.Tests/postgres.local.json` and provide an admin connection string for a PostgreSQL server where the test user can:

- connect successfully
- create and drop databases
- create the `pg_trgm` extension inside the temporary test database

Example:

```json
{
  "AdminConnectionString": "Host=localhost;Database=postgres;Username=test;Password=test",
  "DatabaseNamePrefix": "portable_text_search_tests"
}
```

Each test run creates a fresh database with a unique name derived from `DatabaseNamePrefix`, runs the EF migration and query workflow, and drops the database in teardown.

Environment variables are still supported as an override:

- `PORTABLE_TEXT_SEARCH_POSTGRES_ADMIN_CONNECTION`
- `PORTABLE_TEXT_SEARCH_POSTGRES_DATABASE_NAME`

## SQLite limitation

SQLite query translation now targets FTS5 directly, and migration helpers generate the virtual table plus synchronization triggers. The current first version assumes the default virtual table naming convention from the migration helper when translating LINQ queries. If you choose a custom SQLite virtual table name, you should treat that as an advanced scenario until model-level naming configuration is added.

## Next direction

The current architecture leaves room for future SQLite FTS5 support by keeping:

- provider-neutral public APIs
- provider-specific query translators
- separate migration helper entry points
