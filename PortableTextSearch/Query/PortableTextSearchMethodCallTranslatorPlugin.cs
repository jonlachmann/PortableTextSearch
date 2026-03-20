using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
{
    public PortableTextSearchMethodCallTranslatorPlugin(
        IDatabaseProvider databaseProvider,
        ICurrentDbContext currentDbContext,
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        Translators =
        [
            new PortableTextSearchMethodCallTranslator(databaseProvider, currentDbContext, sqlExpressionFactory, typeMappingSource)
        ];
    }

    public IEnumerable<IMethodCallTranslator> Translators { get; }
}
