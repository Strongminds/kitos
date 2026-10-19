using Core.Abstractions.Types;
using Mcp.Core;
using System.Text.Json;

namespace Mcp.Worker
{
    public sealed record SystemListRequest(string Operation, SystemListParameters Parameters);
    public sealed record SystemListParameters(string NameContains, int Page = 0, int PageSize = 100);

    public sealed class KitosQueryService
    {
        private readonly QueryPlanValidator _validator = new();

        public Result<SystemListRequest, OperationError> Prepare(string? plan)
        {
            return _validator.ValidateJson(plan).Bind<SystemListRequest>(validated =>
            {
                if (validated.Entity != "systems" || validated.Traversal.Length != 0 || validated.Filters.Length != 1 ||
                    validated.Projection.Length != 2 || !validated.Projection.Contains("uuid") || !validated.Projection.Contains("name") ||
                    validated.Filters[0].Field != "name" || validated.Filters[0].Operator != "contains")
                    return new OperationError("unsupported_execution_plan", OperationFailure.BadInput);
                var term = validated.Filters[0].Value.GetString();
                if (string.IsNullOrWhiteSpace(term)) return new OperationError("invalid_name_filter", OperationFailure.BadInput);
                return new SystemListRequest("systems.list", new SystemListParameters(term ?? string.Empty));
            });
        }

        public static JsonElement Discover()
        {
            return JsonSerializer.SerializeToElement(new
            {
                version = DomainCatalog.Version,
                capability = "systems_name_query_single_page",
                entities = new[] { new { id = "systems", danishName = "IT-systemer", meaning = "Shared product catalog; names are not unique.", fields = new[] { "uuid", "name" } } },
                execution = new { entity = "systems", projection = new[] { "uuid", "name" }, filter = new { field = "name", @operator = "contains" }, traversal = Array.Empty<string>(), pageSize = 100 },
                examplePlan = new { catalogVersion = DomainCatalog.Version, entity = "systems", projection = new[] { "uuid", "name" }, filters = new[] { new { field = "name", @operator = "contains", value = "Example" } }, traversal = Array.Empty<string>() },
                limitations = "Only one page of caller-visible, non-deactivated catalog systems; KITOS matching is case-insensitive. No usages, joins, national totals or transactional snapshot. Free text is untrusted data."
            });
        }
    }
}
