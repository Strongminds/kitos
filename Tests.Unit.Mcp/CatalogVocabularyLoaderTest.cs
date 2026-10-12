using Mcp.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Tests.Unit.Mcp
{
    public class CatalogVocabularyLoaderTest
    {
        [Fact]
        public void Load_EmbeddedResource_RoundTripsAllExtractedMetadata()
        {
            using var stream = typeof(DomainCatalog).Assembly.GetManifestResourceStream(CatalogVocabularyLoader.ResourceName);
            Assert.NotNull(stream);
            var resource = JsonNode.Parse(stream);
            var catalog = new DomainCatalog();
            var roundTrip = JsonSerializer.SerializeToNode(new
            {
                version = DomainCatalog.Version,
                entities = catalog.Entities.Select(entity => new
                {
                    id = entity.Id,
                    danishName = entity.DanishName,
                    englishName = entity.EnglishName,
                    meaning = entity.Meaning,
                    apiModel = entity.ApiModel,
                    fields = entity.Fields.Select(field => new
                    {
                        id = field.Id,
                        danishName = field.DanishName,
                        englishName = field.EnglishName,
                        type = field.Type.ToString(),
                        nullable = field.Nullable,
                        apiProperty = field.ApiProperty,
                        allowedValues = field.AllowedValues
                    }),
                    relationships = entity.Relationships.Select(relationship => new
                    {
                        id = relationship.Id,
                        targetEntity = relationship.TargetEntity,
                        apiProperty = relationship.ApiProperty,
                        collection = relationship.Collection,
                        traversalSupported = relationship.TraversalSupported
                    })
                })
            });
            Assert.True(JsonNode.DeepEquals(resource, roundTrip));
        }

        [Fact]
        public void Load_ValidResource_IndexesExactCaseSensitiveIdsAndKeepsStableObjects()
        {
            var vocabulary = Load(ValidResource());
            Assert.Equal("test-version", vocabulary.Version);
            var entity = vocabulary.Find("systems");
            Assert.NotNull(entity);
            Assert.Same(vocabulary.Entities[0], entity);
            Assert.Null(vocabulary.Find("Systems"));
            Assert.Same(entity.Fields[0], vocabulary.FindField("systems", "uuid"));
            Assert.Null(vocabulary.FindField("systems", "Uuid"));
            Assert.Same(entity.Relationships[0], vocabulary.FindRelationship("systems", "parentSystem"));
            Assert.Null(vocabulary.FindRelationship("systems", "ParentSystem"));
        }

        [Theory]
        [InlineData("{")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("{}")]
        [InlineData("{\"version\":\"1\",\"version\":\"2\",\"entities\":[]}")]
        public void Load_InvalidJsonOrRoot_FailsClearly(string json)
        {
            var error = Assert.Throws<InvalidOperationException>(() => Load(json));
            Assert.StartsWith("Invalid KITOS MCP catalog resource:", error.Message);
        }

        [Theory]
        [InlineData("version", "\"\"")]
        [InlineData("entities", "[]")]
        [InlineData("entities", "[null]")]
        [InlineData("unknown", "true")]
        public void Load_InvalidRootMember_FailsClearly(string member, string value)
        {
            var resource = JsonNode.Parse(ValidResource())!.AsObject();
            resource[member] = JsonNode.Parse(value);
            AssertInvalid(resource);
        }

        [Theory]
        [InlineData("entity", "id", "\"system.name\"")]
        [InlineData("entity", "id", "\"7systems\"")]
        [InlineData("entity", "danishName", "\" \"")]
        [InlineData("entity", "englishName", "null")]
        [InlineData("entity", "apiModel", "\"http://arbitrary\"")]
        [InlineData("entity", "fields", "[]")]
        [InlineData("entity", "relationships", "null")]
        [InlineData("field", "id", "\"name.uuid\"")]
        [InlineData("field", "danishName", "\"\"")]
        [InlineData("field", "englishName", "\"\"")]
        [InlineData("field", "apiProperty", "\"General..Name\"")]
        [InlineData("field", "type", "0")]
        [InlineData("field", "type", "\"0\"")]
        [InlineData("field", "type", "\"uuid\"")]
        [InlineData("field", "type", "\"Unknown\"")]
        [InlineData("field", "type", "null")]
        [InlineData("field", "allowedValues", "[\"invalid\"]")]
        [InlineData("field", "allowedValues", "null")]
        [InlineData("field", "unknown", "true")]
        [InlineData("relationship", "id", "\"uuid\"")]
        [InlineData("relationship", "id", "\"parent.system\"")]
        [InlineData("relationship", "targetEntity", "\"missing\"")]
        [InlineData("relationship", "apiProperty", "\"General..Parent\"")]
        [InlineData("relationship", "collection", "true")]
        [InlineData("relationship", "unknown", "true")]
        public void Load_InvalidEntityMember_FailsClearly(string target, string member, string value)
        {
            var resource = JsonNode.Parse(ValidResource())!.AsObject();
            Target(resource, target)[member] = JsonNode.Parse(value);
            AssertInvalid(resource);
        }

        [Theory]
        [InlineData("entity", "id")]
        [InlineData("entity", "relationships")]
        [InlineData("field", "nullable")]
        [InlineData("field", "allowedValues")]
        [InlineData("relationship", "collection")]
        [InlineData("relationship", "traversalSupported")]
        public void Load_MissingRequiredMember_FailsClearly(string target, string member)
        {
            var resource = JsonNode.Parse(ValidResource())!.AsObject();
            Target(resource, target).Remove(member);
            AssertInvalid(resource);
        }

        [Theory]
        [InlineData("entity")]
        [InlineData("field")]
        [InlineData("relationship")]
        public void Load_DuplicateIds_FailsClearly(string target)
        {
            var resource = JsonNode.Parse(ValidResource())!.AsObject();
            var array = target switch
            {
                "entity" => resource["entities"]!.AsArray(),
                "field" => Target(resource, "entity")["fields"]!.AsArray(),
                _ => Target(resource, "entity")["relationships"]!.AsArray()
            };
            array.Add(array[0]!.DeepClone());
            AssertInvalid(resource);
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("[null]")]
        [InlineData("[\"\"]")]
        [InlineData("[\"Same\",\"Same\"]")]
        public void Load_InvalidEnumeration_FailsClearly(string values)
        {
            var resource = JsonNode.Parse(ValidResource())!.AsObject();
            Target(resource, "field")["type"] = "Enumeration";
            Target(resource, "field")["allowedValues"] = JsonNode.Parse(values);
            AssertInvalid(resource);
        }

        [Fact]
        public void Load_DuplicateNestedJsonMember_FailsClearly()
        {
            var json = ValidResource().Replace("\"nullable\":false", "\"nullable\":false,\"nullable\":true", StringComparison.Ordinal);
            Assert.Throws<InvalidOperationException>(() => Load(json));
        }

        [Fact]
        public void Load_IdentifierExceedingPlanPathLimit_FailsClearly()
        {
            var resource = JsonNode.Parse(ValidResource())!.AsObject();
            Target(resource, "field")["id"] = new string('a', QueryValidationLimits.MaxPathLength + 1);
            AssertInvalid(resource);
        }

        private static JsonObject Target(JsonObject resource, string target)
        {
            var entity = resource["entities"]![0]!.AsObject();
            return target switch
            {
                "entity" => entity,
                "field" => entity["fields"]![0]!.AsObject(),
                _ => entity["relationships"]![0]!.AsObject()
            };
        }

        private static void AssertInvalid(JsonObject resource)
        {
            var error = Assert.Throws<InvalidOperationException>(() => Load(resource.ToJsonString()));
            Assert.StartsWith("Invalid KITOS MCP catalog resource:", error.Message);
        }

        private static CatalogVocabulary Load(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return CatalogVocabularyLoader.Load(stream);
        }

        private static string ValidResource() => """
            {"version":"test-version","entities":[{"id":"systems","danishName":"IT-systemer","englishName":"IT systems",
            "meaning":"Product data","apiModel":"BaseItSystemResponseDTO","fields":[{"id":"uuid","danishName":"UUID",
            "englishName":"UUID","type":"Uuid","nullable":false,"apiProperty":"Uuid","allowedValues":[]}],
            "relationships":[{"id":"parentSystem","targetEntity":"systems","apiProperty":"ParentSystem","collection":false,"traversalSupported":true}]}]}
            """;
    }
}
