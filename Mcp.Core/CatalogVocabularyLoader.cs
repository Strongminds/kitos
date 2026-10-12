using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcp.Core
{
    internal sealed class CatalogVocabulary
    {
        private readonly FrozenDictionary<string, EntityDefinition> _entities;
        private readonly FrozenDictionary<string, FrozenDictionary<string, FieldDefinition>> _fields;
        private readonly FrozenDictionary<string, FrozenDictionary<string, RelationshipDefinition>> _relationships;

        internal CatalogVocabulary(string version, ImmutableArray<EntityDefinition> entities)
        {
            Version = version;
            Entities = entities;
            _entities = entities.ToFrozenDictionary(entity => entity.Id, StringComparer.Ordinal);
            _fields = entities.ToFrozenDictionary(entity => entity.Id,
                entity => entity.Fields.ToFrozenDictionary(field => field.Id, StringComparer.Ordinal), StringComparer.Ordinal);
            _relationships = entities.ToFrozenDictionary(entity => entity.Id,
                entity => entity.Relationships.ToFrozenDictionary(relationship => relationship.Id, StringComparer.Ordinal), StringComparer.Ordinal);
        }

        internal string Version { get; }
        internal ImmutableArray<EntityDefinition> Entities { get; }
        internal EntityDefinition? Find(string? id) => id is not null && _entities.TryGetValue(id, out var entity) ? entity : null;
        internal FieldDefinition? FindField(string entityId, string id) => _fields[entityId].GetValueOrDefault(id);
        internal RelationshipDefinition? FindRelationship(string entityId, string id) => _relationships[entityId].GetValueOrDefault(id);
    }

    /// <summary>Validates packaged metadata once. Invalid packaged vocabulary is a startup/configuration error, not a query error.</summary>
    internal static class CatalogVocabularyLoader
    {
        internal const string ResourceName = "Mcp.Core.Resources.domain-catalog.v1.json";
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            MaxDepth = 16
        };

        internal static CatalogVocabulary LoadDefault()
        {
            using var stream = typeof(DomainCatalog).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw Invalid("embedded resource is missing");
            return Load(stream);
        }

        internal static CatalogVocabulary Load(Stream stream)
        {
            try
            {
                using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { MaxDepth = 16 });
                Require(!HasDuplicateMembers(document.RootElement), "duplicate JSON member");
                var resource = document.RootElement.Deserialize<VocabularyResource>(SerializerOptions);
                Require(resource is not null, "resource must be an object");
                return Validate(resource);
            }
            catch (JsonException exception)
            {
                throw Invalid("malformed or unsupported JSON", exception);
            }
        }

        private static CatalogVocabulary Validate(VocabularyResource resource)
        {
            Require(!string.IsNullOrWhiteSpace(resource.Version), "version is required");
            Require(!resource.Entities.IsDefaultOrEmpty, "entities are required");
            var entityIds = new HashSet<string>(StringComparer.Ordinal);
            var definitions = ImmutableArray.CreateBuilder<EntityDefinition>(resource.Entities.Length);
            foreach (var entity in resource.Entities)
            {
                Require(entity is not null, "null entity");
                Require(IsIdentifier(entity.Id) && entityIds.Add(entity.Id), "entity IDs must be legal and unique");
                Require(HasText(entity.DanishName, entity.EnglishName, entity.Meaning), "entity text is required");
                Require(IsIdentifier(entity.ApiModel), "invalid API model name");
                Require(!entity.Fields.IsDefaultOrEmpty && !entity.Relationships.IsDefault, "field and relationship arrays are required");
                var memberIds = new HashSet<string>(StringComparer.Ordinal);
                var fields = ImmutableArray.CreateBuilder<FieldDefinition>(entity.Fields.Length);
                foreach (var field in entity.Fields)
                {
                    Require(field is not null, "null field");
                    Require(IsIdentifier(field.Id) && memberIds.Add(field.Id), "field IDs must be legal and unique");
                    Require(HasText(field.DanishName, field.EnglishName), "bilingual field text is required");
                    Require(IsPropertyPath(field.ApiProperty), "invalid field API property path");
                    Require(Enum.TryParse<FieldType>(field.Type, false, out var type) && Enum.GetName(type) == field.Type,
                        "field type must use a supported string name");
                    Require(!field.AllowedValues.IsDefault, "allowedValues array is required");
                    Require(type == FieldType.Enumeration ? !field.AllowedValues.IsEmpty : field.AllowedValues.IsEmpty,
                        "only enum fields must define allowed values");
                    Require(field.AllowedValues.All(value => !string.IsNullOrWhiteSpace(value)) &&
                        field.AllowedValues.Distinct(StringComparer.Ordinal).Count() == field.AllowedValues.Length,
                        "enum values must be nonempty and unique");
                    fields.Add(new FieldDefinition(field.Id, field.DanishName, field.EnglishName, type,
                        field.Nullable, field.ApiProperty, field.AllowedValues));
                }

                foreach (var relationship in entity.Relationships)
                {
                    Require(relationship is not null, "null relationship");
                    Require(IsIdentifier(relationship.Id) && memberIds.Add(relationship.Id), "relationship IDs must be legal and unique across entity members");
                    Require(IsIdentifier(relationship.TargetEntity), "invalid relationship target ID");
                    Require(IsPropertyPath(relationship.ApiProperty), "invalid relationship API property path");
                    Require(!relationship.Collection || !relationship.TraversalSupported, "collection traversal is unsupported");
                }

                definitions.Add(new EntityDefinition(entity.Id, entity.DanishName, entity.EnglishName, entity.Meaning,
                    entity.ApiModel, fields.ToImmutable(), entity.Relationships));
            }

            foreach (var entity in definitions)
            {
                Require(entity.Relationships.All(relationship => entityIds.Contains(relationship.TargetEntity)), "unknown relationship target");
            }

            return new CatalogVocabulary(resource.Version, definitions.ToImmutable());
        }

        private static bool HasText(params string[] values) => values.All(value => !string.IsNullOrWhiteSpace(value));
        private static bool IsPropertyPath(string? value) => value is not null && value.Split('.').All(IsIdentifier);
        private static bool IsIdentifier(string? value) => !string.IsNullOrEmpty(value) && value.Length <= QueryValidationLimits.MaxPathLength &&
            (char.IsAsciiLetter(value[0]) || value[0] == '_') && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
        private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
        {
            if (!condition) throw Invalid(reason);
        }
        private static InvalidOperationException Invalid(string reason, Exception? inner = null) =>
            new($"Invalid KITOS MCP catalog resource: {reason}.", inner);

        private static bool HasDuplicateMembers(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name) || HasDuplicateMembers(property.Value)) return true;
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    if (HasDuplicateMembers(item)) return true;
                }
            }
            return false;
        }

        private sealed record VocabularyResource(string Version, ImmutableArray<VocabularyEntity> Entities);
        private sealed record VocabularyEntity(string Id, string DanishName, string EnglishName, string Meaning,
            string ApiModel, ImmutableArray<VocabularyField> Fields, ImmutableArray<RelationshipDefinition> Relationships);
        private sealed record VocabularyField(string Id, string DanishName, string EnglishName, string Type,
            bool Nullable, string ApiProperty, ImmutableArray<string> AllowedValues);
    }
}
