namespace Tests.Infrastructure.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MigrationTestsCollection
{
    public const string Name = "Migration tests";
}
