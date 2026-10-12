using Core.Abstractions.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcp.Core
{
    public sealed class QueryPlanValidator
    {
        private readonly DomainCatalog _catalog;
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            MaxDepth = 8
        };

        public QueryPlanValidator(DomainCatalog? catalog = null)
        {
            _catalog = catalog ?? new DomainCatalog();
        }

        public Result<ValidatedQueryPlan, OperationError> ValidateJson(string? json)
        {
            if (json is null || Encoding.UTF8.GetByteCount(json) > QueryValidationLimits.MaxJsonBytes)
            {
                return Failure("plan_size", "$");
            }

            try
            {
                using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
                if (HasDuplicateMembers(document.RootElement))
                {
                    return Failure("invalid_json", "$");
                }

                return Validate(document.RootElement.Deserialize<QueryPlan>(SerializerOptions));
            }
            catch (JsonException)
            {
                // Never echo supplied JSON: it may contain credentials or injection attempts.
                return Failure("invalid_json", "$");
            }
        }

        public Result<ValidatedQueryPlan, OperationError> Validate(QueryPlan? plan)
        {
            if (plan is null) return Failure("invalid_plan", "$");
            if (plan.CatalogVersion != DomainCatalog.Version) return Failure("catalog_version", "catalogVersion");
            var entity = _catalog.Find(plan.Entity);
            if (entity is null) return Failure("unknown_entity", "entity");
            if (plan.Projection.IsDefault || plan.Projection.Length == 0 || plan.Projection.Length > QueryValidationLimits.MaxProjectionFields)
                return Failure("projection_limit", "projection");
            if (plan.Filters.IsDefault || plan.Filters.Length > QueryValidationLimits.MaxFilters)
                return Failure("filter_limit", "filters");
            if (plan.Traversal.IsDefault || plan.Traversal.Length > QueryValidationLimits.MaxTraversals)
                return Failure("traversal_limit", "traversal");

            var traversals = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < plan.Traversal.Length; index++)
            {
                var path = plan.Traversal[index];
                var error = ValidateTraversal(entity, path);
                if (error is not null) return Failure(error, $"traversal[{index}]");
                if (!traversals.Add(path)) return Failure("duplicate_traversal", $"traversal[{index}]");
            }

            foreach (var path in traversals)
            {
                var parent = path.LastIndexOf('.');
                if (parent >= 0 && !traversals.Contains(path[..parent]))
                    return Failure("missing_traversal", "traversal");
            }

            var projection = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < plan.Projection.Length; index++)
            {
                var path = plan.Projection[index];
                var field = ResolveField(entity, path, traversals);
                if (field is null) return Failure("unsupported_field", $"projection[{index}]");
                if (!projection.Add(path)) return Failure("duplicate_projection", $"projection[{index}]");
            }

            var safeFilters = System.Collections.Immutable.ImmutableArray.CreateBuilder<QueryFilter>(plan.Filters.Length);
            for (var index = 0; index < plan.Filters.Length; index++)
            {
                var filter = plan.Filters[index];
                if (filter is null) return Failure("invalid_filter", $"filters[{index}]");
                var field = ResolveField(entity, filter.Field, traversals);
                if (field is null) return Failure("unsupported_field", $"filters[{index}].field");
                if (!field.SupportedOperators.Contains(filter.Operator, StringComparer.Ordinal)) return Failure("unsupported_operator", $"filters[{index}].operator");
                JsonElement value;
                try
                {
                    if (filter.Value.ValueKind == JsonValueKind.Undefined) return Failure("invalid_value", $"filters[{index}].value");
                    value = filter.Value.Clone();
                }
                catch (ObjectDisposedException)
                {
                    return Failure("invalid_value", $"filters[{index}].value");
                }
                if (!SupportsValue(field, filter.Operator, value)) return Failure("invalid_value", $"filters[{index}].value");
                safeFilters.Add(filter with { Value = value });
            }

            return new ValidatedQueryPlan(plan with { Filters = safeFilters.ToImmutable() });
        }

        private string? ValidateTraversal(EntityDefinition entity, string? path)
        {
            if (string.IsNullOrEmpty(path) || path.Length > QueryValidationLimits.MaxPathLength) return "invalid_path";
            var parts = path.Split('.');
            if (parts.Length > QueryValidationLimits.MaxTraversalDepth) return "traversal_depth";
            foreach (var part in parts)
            {
                var relationship = _catalog.FindRelationship(entity, part);
                if (relationship is null) return "unknown_relationship";
                if (!relationship.TraversalSupported) return "unsupported_relationship";
                entity = _catalog.Find(relationship.TargetEntity) ?? throw new InvalidOperationException("Invalid built-in catalog target.");
            }

            return null;
        }

        private FieldDefinition? ResolveField(EntityDefinition entity, string? path, HashSet<string> traversals)
        {
            if (string.IsNullOrEmpty(path) || path.Length > QueryValidationLimits.MaxPathLength) return null;
            var parts = path.Split('.');
            if (parts.Length > QueryValidationLimits.MaxTraversalDepth + 1) return null;
            if (parts.Length > 1)
            {
                var relationshipPath = path[..path.LastIndexOf('.')];
                if (!traversals.Contains(relationshipPath)) return null;
                foreach (var part in parts[..^1])
                {
                    var relationship = _catalog.FindRelationship(entity, part);
                    if (relationship is null || !relationship.TraversalSupported) return null;
                    var target = _catalog.Find(relationship.TargetEntity);
                    if (target is null) return null;
                    entity = target;
                }
            }

            return _catalog.FindField(entity, parts[^1]);
        }

        private static bool SupportsValue(FieldDefinition field, string operation, JsonElement value)
        {
            if (operation == "isNull") return value.ValueKind is JsonValueKind.True or JsonValueKind.False;
            if (value.ValueKind == JsonValueKind.Null) return false; // Null tests must be explicit.
            if (field.Type == FieldType.Boolean) return value.ValueKind is JsonValueKind.True or JsonValueKind.False;
            if (value.ValueKind != JsonValueKind.String) return false;
            var text = value.GetString();
            if (text is null || text.Length > QueryValidationLimits.MaxTextValueLength) return false;
            return field.Type switch
            {
                FieldType.Uuid => Guid.TryParseExact(text, "D", out _),
                FieldType.Timestamp => DateTimeOffset.TryParseExact(text,
                    ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"],
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                FieldType.Enumeration => field.AllowedValues.Contains(text, StringComparer.Ordinal),
                FieldType.Text => operation != "contains" || text.Length > 0,
                _ => false
            };
        }

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
                foreach (var element in value.EnumerateArray())
                {
                    if (HasDuplicateMembers(element)) return true;
                }
            }

            return false;
        }

        private static Result<ValidatedQueryPlan, OperationError> Failure(string code, string path) => new PlanValidationError(code, path);
    }
}

