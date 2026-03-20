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

## SQLite limitation

SQLite query translation now targets FTS5 directly, and migration helpers generate the virtual table plus synchronization triggers. The current first version assumes the default virtual table naming convention from the migration helper when translating LINQ queries. If you choose a custom SQLite virtual table name, you should treat that as an advanced scenario until model-level naming configuration is added.

## Next direction

The current architecture leaves room for future SQLite FTS5 support by keeping:

- provider-neutral public APIs
- provider-specific query translators
- separate migration helper entry points
