using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortableTextSearch.Configuration;

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
    }
}
