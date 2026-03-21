using Xunit;

namespace PortableTextSearch.Tests;

/// <summary>
/// Skips PostgreSQL integration tests unless local PostgreSQL test settings are available.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public RequiresPostgresFactAttribute()
    {
        if (!PostgreSqlTestConfiguration.IsConfigured())
        {
            Skip = "Configure PostgreSQL test settings via PortableTextSearch.Tests/postgres.local.json or environment variables to run PostgreSQL integration tests.";
        }
    }
}
