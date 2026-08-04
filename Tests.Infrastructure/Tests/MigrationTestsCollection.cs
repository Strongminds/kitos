using Tests.Infrastructure.Helpers;

namespace Tests.Infrastructure.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MigrationTestsCollection : ICollectionFixture<ContainerRuntimeFixture>
{
    public const string Name = "Migration tests";
}
