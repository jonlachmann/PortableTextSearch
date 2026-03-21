#pragma warning disable EF1001

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;
using PortableTextSearch.Configuration;
using PortableTextSearch.Internal;
using PortableTextSearch.Migrations.Operations;

namespace PortableTextSearch.Migrations;

internal sealed class PortableTextSearchMigrationsModelDiffer(
    IRelationalTypeMappingSource typeMappingSource,
    IMigrationsAnnotationProvider migrationsAnnotationProvider,
#if !PORTABLETEXTSEARCH_EF8
    IRelationalAnnotationProvider relationalAnnotationProvider,
#endif
    IRowIdentityMapFactory rowIdentityMapFactory,
    CommandBatchPreparerDependencies commandBatchPreparerDependencies,
    IDatabaseProvider databaseProvider)
    : MigrationsModelDiffer(
        typeMappingSource,
        migrationsAnnotationProvider,
#if !PORTABLETEXTSEARCH_EF8
        relationalAnnotationProvider,
#endif
        rowIdentityMapFactory,
        commandBatchPreparerDependencies)
{
    private readonly IDatabaseProvider _databaseProvider = databaseProvider;

    public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target)
    {
        var dropOperations = source is null
            ? Array.Empty<MigrationOperation>()
            : BuildDropOperations(source, target);
        var baseOperations = base.GetDifferences(source!, target!);
        var createOperations = target is null
            ? Array.Empty<MigrationOperation>()
            : BuildCreateOperations(source, target);

        return dropOperations
            .Concat(baseOperations)
            .Concat(createOperations)
            .ToArray();
    }

    public override bool HasDifferences(IRelationalModel? source, IRelationalModel? target)
    {
        return base.HasDifferences(source!, target!)
               || source is not null && BuildDropOperations(source, target).Count != 0
               || target is not null && BuildCreateOperations(source, target).Count != 0;
    }

    private IReadOnlyList<MigrationOperation> BuildDropOperations(IRelationalModel source, IRelationalModel? target)
    {
        var sourceConfigurations = GetConfigurations(source);
        var targetConfigurations = GetConfigurations(target);
        var droppedKeys = sourceConfigurations.Keys
            .Where(key => !targetConfigurations.TryGetValue(key, out var targetConfiguration)
                          || !ConfigurationEquals(sourceConfigurations[key], targetConfiguration))
            .ToArray();

        return droppedKeys
            .SelectMany(key => CreateDropOperations(sourceConfigurations[key]))
            .ToArray();
    }

    private IReadOnlyList<MigrationOperation> BuildCreateOperations(IRelationalModel? source, IRelationalModel target)
    {
        var sourceConfigurations = GetConfigurations(source);
        var targetConfigurations = GetConfigurations(target);
        var addedKeys = targetConfigurations.Keys
            .Where(key => !sourceConfigurations.TryGetValue(key, out var sourceConfiguration)
                          || !ConfigurationEquals(sourceConfiguration, targetConfigurations[key]))
            .ToArray();

        var operations = new List<MigrationOperation>();

        if (_databaseProvider.Name == ProviderNames.Npgsql && addedKeys.Length != 0)
        {
            operations.Add(new EnsurePostgresTrigramExtensionOperation());
        }

        foreach (var key in addedKeys)
        {
            operations.AddRange(CreateCreateOperations(targetConfigurations[key]));
        }

        return operations;
    }

    private Dictionary<TextSearchTableKey, TextSearchTableConfiguration> GetConfigurations(IRelationalModel? relationalModel)
    {
        var configurations = new Dictionary<TextSearchTableKey, TextSearchTableConfiguration>();
        if (relationalModel is null)
        {
            return configurations;
        }

        foreach (var entityType in relationalModel.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            var schema = entityType.GetSchema();
            if (_databaseProvider.Name == ProviderNames.Sqlite && !string.IsNullOrEmpty(schema))
            {
                continue;
            }

            var storeObject = StoreObjectIdentifier.Table(tableName, schema);
            var searchColumns = entityType.GetTextSearchProperties()
                .Select(propertyName => entityType.FindProperty(propertyName))
                .Where(property => property is not null)
                .Select(property => property!.GetColumnName(storeObject))
                .Where(columnName => !string.IsNullOrWhiteSpace(columnName))
                .Select(columnName => columnName!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(columnName => columnName, StringComparer.Ordinal)
                .ToArray();

            if (searchColumns.Length == 0)
            {
                continue;
            }

            var keyProperty = entityType.FindPrimaryKey()?.Properties.SingleOrDefault();
            var keyColumnName = keyProperty?.GetColumnName(storeObject);
            if (keyProperty is null || string.IsNullOrWhiteSpace(keyColumnName))
            {
                continue;
            }

            var key = new TextSearchTableKey(tableName, schema);
            if (configurations.TryGetValue(key, out var existing))
            {
                configurations[key] = existing.Merge(searchColumns);
                continue;
            }

            configurations[key] = new TextSearchTableConfiguration(
                tableName,
                schema,
                searchColumns,
                keyColumnName!);
        }

        return configurations;
    }

    private IReadOnlyList<MigrationOperation> CreateCreateOperations(TextSearchTableConfiguration configuration)
        => _databaseProvider.Name switch
        {
            ProviderNames.Npgsql => configuration.SearchColumns
                .Select(column => new CreatePostgresTextSearchIndexOperation
                {
                    Table = configuration.Table,
                    Column = column,
                    Schema = configuration.Schema
                })
                .ToArray(),
            ProviderNames.Sqlite => [new CreateSqliteTextSearchIndexOperation
            {
                Table = configuration.Table,
                Columns = configuration.SearchColumns,
                ContentKeyColumn = configuration.KeyColumnName
            }],
            _ => []
        };

    private IReadOnlyList<MigrationOperation> CreateDropOperations(TextSearchTableConfiguration configuration)
        => _databaseProvider.Name switch
        {
            ProviderNames.Npgsql => configuration.SearchColumns
                .Select(column => new DropPostgresTextSearchIndexOperation
                {
                    Table = configuration.Table,
                    Column = column,
                    Schema = configuration.Schema
                })
                .ToArray(),
            ProviderNames.Sqlite => [new DropSqliteTextSearchIndexOperation
            {
                Table = configuration.Table,
                Columns = configuration.SearchColumns,
                ContentKeyColumn = configuration.KeyColumnName
            }],
            _ => []
        };

    private static bool ConfigurationEquals(
        TextSearchTableConfiguration left,
        TextSearchTableConfiguration right)
        => string.Equals(left.Table, right.Table, StringComparison.Ordinal)
           && string.Equals(left.Schema, right.Schema, StringComparison.Ordinal)
           && string.Equals(left.KeyColumnName, right.KeyColumnName, StringComparison.Ordinal)
           && left.SearchColumns.SequenceEqual(right.SearchColumns, StringComparer.Ordinal);

    private readonly record struct TextSearchTableKey(string Table, string? Schema);

    private sealed record TextSearchTableConfiguration(
        string Table,
        string? Schema,
        IReadOnlyList<string> SearchColumns,
        string KeyColumnName)
    {
        public TextSearchTableConfiguration Merge(IReadOnlyList<string> additionalColumns)
            => this with
            {
                SearchColumns = SearchColumns
                    .Concat(additionalColumns)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(columnName => columnName, StringComparer.Ordinal)
                    .ToArray()
            };
    }
}

#pragma warning restore EF1001
