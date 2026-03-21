using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortableTextSearch.Configuration;
using PortableTextSearch.Migrations;

namespace PortableTextSearch.Design;

/// <summary>
/// Registers PortableTextSearch design-time services used when EF Core scaffolds migrations and snapshots.
/// </summary>
public sealed class PortableTextSearchDesignTimeServices : IDesignTimeServices
{
    /// <summary>
    /// Adds PortableTextSearch design-time services to the given service collection.
    /// </summary>
    /// <param name="services">The design-time service collection.</param>
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IAnnotationCodeGenerator, PortableTextSearchAnnotationCodeGenerator>());
        services.Replace(ServiceDescriptor.Singleton<ICSharpMigrationOperationGenerator, PortableTextSearchCSharpMigrationOperationGenerator>());
        services.Replace(ServiceDescriptor.Singleton<IMigrationsCodeGenerator, PortableTextSearchCSharpMigrationsGenerator>());
    }
}
