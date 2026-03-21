using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using PortableTextSearch.Configuration;
using PortableTextSearch.Migrations;

namespace PortableTextSearch.Query;

internal sealed class PortableTextSearchOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IMethodCallTranslatorPlugin, PortableTextSearchMethodCallTranslatorPlugin>());
        services.Replace(ServiceDescriptor.Scoped<IQuerySqlGeneratorFactory, PortableTextSearchQuerySqlGeneratorFactory>());
        services.Replace(ServiceDescriptor.Scoped<IRelationalParameterBasedSqlProcessorFactory, PortableTextSearchRelationalParameterBasedSqlProcessorFactory>());
        services.Replace(ServiceDescriptor.Scoped<IMigrationsModelDiffer, PortableTextSearchMigrationsModelDiffer>());
        services.Replace(ServiceDescriptor.Singleton<IAnnotationCodeGenerator, PortableTextSearchAnnotationCodeGenerator>());
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using PortableTextSearch ";

        public override int GetServiceProviderHashCode() => 0;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo[nameof(PortableTextSearchOptionsExtension)] = "1";

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;
    }
}
