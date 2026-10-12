using Core.Abstractions.Types;
using Mcp.Core;
using System.Linq;
using Xunit;

namespace Tests.Unit.Mcp
{
    public class DomainCatalogTest
    {
        [Fact]
        public void Discover_SeparateCatalogInstances_ShareImmutableVocabularyObjects()
        {
            var first = new DomainCatalog();
            var second = new DomainCatalog();
            Assert.True(first.Entities == second.Entities);
            Assert.Same(first.Entities[0], second.Entities[0]);
        }

        [Fact]
        public void Discover_EmptyScope_ReturnsSixBilingualValidationConcepts()
        {
            var result = new DomainCatalog().Discover();
            Assert.True(result.Ok);
            Assert.Equal("1.0.0", result.Value.Version);
            Assert.Equal("plan_validation_only", result.Value.Capability);
            Assert.Equal(6, result.Value.Entities.Length);
            Assert.Equal("IT-snitflader", result.Value.Entities.Single(entity => entity.Id == "interfaces").DanishName);
            Assert.Equal("IT-Systemer i anvendelse", result.Value.Entities.Single(entity => entity.Id == "usages").DanishName);
            Assert.Equal("Databehandling", result.Value.Entities.Single(entity => entity.Id == "dataProcessingRegistrations").DanishName);
            Assert.All(result.Value.Entities, entity =>
            {
                Assert.False(string.IsNullOrWhiteSpace(entity.DanishName));
                Assert.False(string.IsNullOrWhiteSpace(entity.EnglishName));
                Assert.Contains(entity.Fields, field => field.Id == "uuid");
            });
        }

        [Fact]
        public void Discover_Scope_ReturnsOnlyRequestedConceptsInCatalogOrder()
        {
            var result = new DomainCatalog().Discover("organizations", "usages");
            Assert.True(result.Ok);
            Assert.Equal(["usages", "organizations"], result.Value.Entities.Select(entity => entity.Id));
            var usage = result.Value.Entities[0];
            Assert.DoesNotContain(usage.Fields, field => field.Id == "name");
            Assert.Equal("SystemContext", usage.Relationships.Single(relation => relation.Id == "system").ApiProperty);
            var type = result.Value.Entities[1].Fields.Single(field => field.Id == "organizationType");
            Assert.Equal(new[] { "Municipality", "CommunityOfInterest", "Company", "OtherPublicAuthority" }, type.AllowedValues.AsEnumerable());
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("SYSTEMS")]
        [InlineData(null)]
        public void Discover_UnknownScope_ReturnsBadInput(string? id)
        {
            var result = new DomainCatalog().Discover([id!]);
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.BadInput, result.Error.FailureType);
        }

        [Fact]
        public void Discover_DuplicateScope_ReturnsBadInput()
        {
            Assert.True(new DomainCatalog().Discover("systems", "systems").Failed);
        }

        [Fact]
        public void Discover_CollectionRelationship_ExplicitlyDoesNotSupportTraversal()
        {
            var contract = new DomainCatalog().Discover("contracts").Value.Entities.Single();
            var relationship = contract.Relationships.Single(relation => relation.Id == "systemUsages");
            Assert.True(relationship.Collection);
            Assert.False(relationship.TraversalSupported);
        }

        [Fact]
        public void Discover_ValidationMetadata_ExposesOperatorsAndFinitePolicy()
        {
            var catalog = new DomainCatalog().Discover().Value;
            var fields = catalog.Entities.Single(entity => entity.Id == "organizations").Fields;
            Assert.Equal(new[] { "eq", "ne", "contains" }, fields.Single(field => field.Id == "name").SupportedOperators.AsEnumerable());
            Assert.Contains("isNull", fields.Single(field => field.Id == "cvr").SupportedOperators);
            Assert.Equal(16, catalog.Validation.MaxFilters);
            Assert.Equal(3, catalog.Validation.MaxTraversalDepth);
            Assert.Contains("AND", catalog.Validation.FilterSemantics);
            Assert.Contains("every traversal prefix", catalog.Validation.PathSemantics);
            Assert.Contains("isNull requires a boolean", catalog.Validation.ValueSemantics);
        }
    }
}
