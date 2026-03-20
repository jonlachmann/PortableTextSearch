using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
    private readonly IQuerySqlGeneratorFactory _innerFactory;

    public PortableTextSearchQuerySqlGeneratorFactory(
        IServiceProvider serviceProvider,
        IDatabaseProvider databaseProvider,
        QuerySqlGeneratorDependencies dependencies)
    {
        _innerFactory = databaseProvider.Name switch
        {
            Internal.ProviderNames.Sqlite => new PortableTextSearchSqliteQuerySqlGeneratorFactory(dependencies),
            Internal.ProviderNames.Npgsql => CreateWithServices<IQuerySqlGeneratorFactory>(
                serviceProvider,
                "Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal.NpgsqlQuerySqlGeneratorFactory, Npgsql.EntityFrameworkCore.PostgreSQL",
                [dependencies]),
            _ => throw new NotSupportedException($"PortableTextSearch does not know how to preserve provider query SQL generation for '{databaseProvider.Name}'.")
        };
    }

    public QuerySqlGenerator Create() => _innerFactory.Create();

    internal static T CreateWithServices<T>(
        IServiceProvider serviceProvider,
        string assemblyQualifiedTypeName,
        object[] explicitArguments)
        where T : class
    {
        var type = Type.GetType(assemblyQualifiedTypeName, throwOnError: true)!;
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(candidate => candidate.GetParameters().Length)
            .First();

        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = explicitArguments.FirstOrDefault(argument => parameters[i].ParameterType.IsInstanceOfType(argument))
                ?? serviceProvider.GetRequiredService(parameters[i].ParameterType);
        }

        return (T)constructor.Invoke(arguments);
    }
}

internal sealed class PortableTextSearchRelationalParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
    private readonly IRelationalParameterBasedSqlProcessorFactory _innerFactory;

    public PortableTextSearchRelationalParameterBasedSqlProcessorFactory(
        IServiceProvider serviceProvider,
        IDatabaseProvider databaseProvider,
        RelationalParameterBasedSqlProcessorDependencies dependencies)
    {
        _innerFactory = databaseProvider.Name switch
        {
            Internal.ProviderNames.Sqlite => new PortableTextSearchSqliteParameterBasedSqlProcessorFactory(dependencies),
            Internal.ProviderNames.Npgsql => PortableTextSearchQuerySqlGeneratorFactory.CreateWithServices<IRelationalParameterBasedSqlProcessorFactory>(
                serviceProvider,
                "Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal.NpgsqlParameterBasedSqlProcessorFactory, Npgsql.EntityFrameworkCore.PostgreSQL",
                [dependencies]),
            _ => throw new NotSupportedException($"PortableTextSearch does not know how to preserve provider SQL processing for '{databaseProvider.Name}'.")
        };
    }

    public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
        => _innerFactory.Create(useRelationalNulls);
}
