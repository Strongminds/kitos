using Core.Abstractions.Types;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Mcp.Core
{
    public enum FieldType { Uuid, Text, Boolean, Timestamp, Enumeration }

    /// <param name="ApiProperty">CLR response DTO property path, not a JSON pointer or executable route.</param>
    public sealed record FieldDefinition(string Id, string DanishName, string EnglishName, FieldType Type,
        bool Nullable, string ApiProperty, ImmutableArray<string> AllowedValues)
    {
        public ImmutableArray<string> SupportedOperators =>
            ["eq", "ne", .. Type == FieldType.Text ? new[] { "contains" } : [],
                .. Type == FieldType.Timestamp ? new[] { "before", "after" } : [], .. Nullable ? new[] { "isNull" } : []];
    }

    public sealed record RelationshipDefinition(string Id, string TargetEntity, string ApiProperty,
        bool Collection, bool TraversalSupported);

    public sealed record EntityDefinition(string Id, string DanishName, string EnglishName, string Meaning,
        string ApiModel, ImmutableArray<FieldDefinition> Fields, ImmutableArray<RelationshipDefinition> Relationships);

    public sealed record CatalogDiscovery(string Version, string Capability, ImmutableArray<EntityDefinition> Entities,
        QueryValidationPolicy Validation);

    public sealed record QueryValidationPolicy(int MaxJsonBytes, int MaxProjectionFields, int MaxFilters,
        int MaxTraversals, int MaxTraversalDepth, int MaxTextValueLength, int MaxPathLength,
        string FilterSemantics, string PathSemantics, string ValueSemantics)
    {
        public static QueryValidationPolicy Current { get; } = new(QueryValidationLimits.MaxJsonBytes,
            QueryValidationLimits.MaxProjectionFields, QueryValidationLimits.MaxFilters, QueryValidationLimits.MaxTraversals,
            QueryValidationLimits.MaxTraversalDepth, QueryValidationLimits.MaxTextValueLength, QueryValidationLimits.MaxPathLength,
            "Filters are combined with AND; no OR, grouping, or aggregation is supported.",
            "Paths use exact case-sensitive relationship IDs separated by dots; declare every traversal prefix before using related fields. Only single-reference traversal is supported.",
            "eq/ne require the field's scalar type and disallow null; isNull requires a boolean (true means null). contains requires nonempty text. UUIDs use D format; timestamps require an explicit offset; enum strings use listed case-sensitive values.");
    }

    /// <summary>Verified V2 response-model concepts. Discovery describes validation, not executable REST mappings.</summary>
    public sealed class DomainCatalog
    {
        private static readonly CatalogVocabulary Vocabulary = CatalogVocabularyLoader.LoadDefault();
        public static string Version => Vocabulary.Version;
        public const string Capability = "plan_validation_only";
        public ImmutableArray<EntityDefinition> Entities => Vocabulary.Entities;
        public Result<CatalogDiscovery, OperationError> Discover(params string[] entityIds)
        {
            if (entityIds is null || entityIds.Length > Entities.Length ||
                entityIds.Any(id => Find(id) is null) || entityIds.Distinct(StringComparer.Ordinal).Count() != entityIds.Length)
            {
                return new OperationError("Catalog scope must contain distinct known entity IDs.", OperationFailure.BadInput);
            }

            return new CatalogDiscovery(Version, Capability,
                entityIds.Length == 0 ? Entities : Entities.Where(entity => entityIds.Contains(entity.Id, StringComparer.Ordinal)).ToImmutableArray(),
                QueryValidationPolicy.Current);
        }

        internal EntityDefinition? Find(string? id) => Vocabulary.Find(id);
        internal FieldDefinition? FindField(EntityDefinition entity, string id) => Vocabulary.FindField(entity.Id, id);
        internal RelationshipDefinition? FindRelationship(EntityDefinition entity, string id) => Vocabulary.FindRelationship(entity.Id, id);
    }
}
