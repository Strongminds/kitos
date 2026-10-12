using Core.Abstractions.Types;
using System.Collections.Immutable;
using System.Text.Json;

namespace Mcp.Core
{
    /// <summary>Filters are combined with AND. A path uses exact relationship IDs followed by a field ID.</summary>
    public sealed record QueryFilter(string Field, string Operator, JsonElement Value);

    public sealed record QueryPlan(string CatalogVersion, string Entity, ImmutableArray<string> Projection,
        ImmutableArray<QueryFilter> Filters, ImmutableArray<string> Traversal);

    /// <summary>Finite validation policy. Runtime paging/requests/duration limits belong to the future executor.</summary>
    public static class QueryValidationLimits
    {
        public const int MaxJsonBytes = 16384;
        public const int MaxProjectionFields = 16;
        public const int MaxFilters = 16;
        public const int MaxTraversals = 8;
        public const int MaxTraversalDepth = 3;
        public const int MaxTextValueLength = 256;
        public const int MaxPathLength = 128;
    }

    public sealed class PlanValidationError : OperationError
    {
        public PlanValidationError(string code, string path) : base($"{code} at {path}", OperationFailure.BadInput)
        {
            Code = code;
            Path = path;
        }

        public string Code { get; }
        public string Path { get; }
    }

    /// <summary>Constructed only by the validator; immutable collections and cloned JSON values preserve its invariants.</summary>
    public sealed class ValidatedQueryPlan
    {
        internal ValidatedQueryPlan(QueryPlan plan)
        {
            CatalogVersion = plan.CatalogVersion;
            Entity = plan.Entity;
            Projection = plan.Projection;
            Traversal = plan.Traversal;
            Filters = [.. System.Linq.Enumerable.Select(plan.Filters, filter => filter with { Value = filter.Value.Clone() })];
        }

        public string CatalogVersion { get; }
        public string Entity { get; }
        public ImmutableArray<string> Projection { get; }
        public ImmutableArray<QueryFilter> Filters { get; }
        public ImmutableArray<string> Traversal { get; }
    }
}
