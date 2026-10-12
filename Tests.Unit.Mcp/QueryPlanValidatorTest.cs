using Core.Abstractions.Types;
using Mcp.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Tests.Unit.Mcp
{
    public class QueryPlanValidatorTest
    {
        [Fact]
        public void ValidateJson_KnownPlan_ReturnsValidatedEntity()
        {
            var result = new QueryPlanValidator().ValidateJson("""
                {"catalogVersion":"1.0.0","entity":"systems","projection":["uuid","name"],"filters":[],"traversal":[]}
                """);

            Assert.True(result.Ok);
            Assert.Equal("systems", result.Value.Entity);
        }

        [Fact]
        public void ValidateJson_ArbitraryUrl_ReturnsSchemaFailure()
        {
            var result = new QueryPlanValidator().ValidateJson("""
                {"catalogVersion":"1.0.0","entity":"systems","projection":["uuid"],"filters":[],"traversal":[],"url":"https://attacker.invalid"}
                """);

            Assert.True(result.Failed);
            Assert.Equal("invalid_json", Assert.IsType<PlanValidationError>(result.Error).Code);
        }

        [Theory]
        [InlineData("{", "invalid_json")]
        [InlineData("null", "invalid_plan")]
        [InlineData("[]", "invalid_json")]
        [InlineData("{}", "invalid_json")]
        [InlineData("{\"entity\":\"systems\",\"entity\":\"contracts\"}", "invalid_json")]
        public void ValidateJson_MalformedInput_FailsClosed(string json, string code)
        {
            AssertFailure(new QueryPlanValidator().ValidateJson(json), code);
        }

        [Theory]
        [InlineData("url")]
        [InlineData("sql")]
        [InlineData("expression")]
        [InlineData("grouping")]
        [InlineData("aggregation")]
        [InlineData("CatalogVersion")]
        public void ValidateJson_UnsupportedTopLevelMember_ReturnsInvalidJson(string member)
        {
            var json = Serialize(Plan());
            json = json[..^1] + ",\"" + member + "\":\"anything\"}";
            AssertFailure(new QueryPlanValidator().ValidateJson(json), "invalid_json");
        }

        [Theory]
        [InlineData("\"projection\":[\"uuid\",null]", "unsupported_field")]
        [InlineData("\"projection\":null", "invalid_json")]
        [InlineData("\"filters\":null", "invalid_json")]
        [InlineData("\"filters\":[null]", "invalid_filter")]
        [InlineData("\"traversal\":null", "invalid_json")]
        [InlineData("\"traversal\":[null]", "invalid_path")]
        [InlineData("\"entity\":7", "invalid_json")]
        [InlineData("\"entity\":null", "invalid_json")]
        [InlineData("\"filters\":[{\"field\":\"name\",\"operator\":7,\"value\":\"x\"}]", "invalid_json")]
        [InlineData("\"filters\":[{\"field\":\"name\",\"operator\":\"eq\"}]", "invalid_json")]
        [InlineData("\"filters\":[{\"field\":\"name\",\"operator\":\"eq\",\"value\":\"x\",\"sql\":\"x\"}]", "invalid_json")]
        [InlineData("\"filters\":[{\"field\":\"name\",\"field\":\"uuid\",\"operator\":\"eq\",\"value\":\"x\"}]", "invalid_json")]
        public void ValidateJson_InvalidMemberShape_RejectsInput(string member, string code)
        {
            using var replacement = JsonDocument.Parse("{" + member + "}");
            var original = JsonNode.Parse(Serialize(Plan()))!.AsObject();
            original.Remove(replacement.RootElement.EnumerateObject().Single().Name);
            var json = original.ToJsonString();
            json = json[..^1] + "," + member + "}";
            AssertFailure(new QueryPlanValidator().ValidateJson(json), code);
        }

        [Fact]
        public void ValidateJson_TooLargeUtf8Input_ReturnsPlanSizeFailure()
        {
            var json = Serialize(Plan()) + new string('æ', QueryValidationLimits.MaxJsonBytes / 2);
            AssertFailure(new QueryPlanValidator().ValidateJson(json), "plan_size");
        }

        [Theory]
        [InlineData("SYSTEMS", "unknown_entity")]
        [InlineData("other", "unknown_entity")]
        public void Validate_UnknownEntity_ReturnsDeterministicFailure(string entity, string code)
        {
            AssertFailure(new QueryPlanValidator().Validate(Plan() with { Entity = entity }), code, "entity");
        }

        [Fact]
        public void Validate_WrongCatalogVersion_FailsBeforeFields()
        {
            AssertFailure(new QueryPlanValidator().Validate(Plan() with { CatalogVersion = "future" }), "catalog_version", "catalogVersion");
        }

        [Theory]
        [InlineData("notes")]
        [InlineData("Name")]
        [InlineData("http://server")]
        [InlineData("organization.name")]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_UnsupportedProjection_FailsClosed(string? field)
        {
            AssertFailure(new QueryPlanValidator().Validate(Plan() with { Projection = [field!] }), "unsupported_field", "projection[0]");
        }

        [Fact]
        public void Validate_MultiHopTraversal_ResolvesFieldOnReferencedEntity()
        {
            var plan = Plan() with
            {
                Entity = "usages",
                Traversal = ["mainContract", "mainContract.organization"],
                Projection = ["uuid", "mainContract.organization.organizationType"],
                Filters = [Filter("mainContract.organization.organizationType", "eq", "Municipality")]
            };
            var result = new QueryPlanValidator().ValidateJson(Serialize(plan));
            Assert.True(result.Ok);
            Assert.Equal("Municipality", result.Value.Filters.Single().Value.GetString());
            Assert.Equal("mainContract.organization.organizationType", result.Value.Projection[1]);
        }

        [Theory]
        [InlineData("unknown", "unknown_relationship")]
        [InlineData("parentSystem.unknown", "unknown_relationship")]
        [InlineData("parentSystem.parentSystem.parentSystem.parentSystem", "traversal_depth")]
        [InlineData("parentSystem.parentSystem", "missing_traversal")]
        [InlineData("", "invalid_path")]
        public void Validate_UnsupportedTraversal_ReturnsFailure(string path, string code)
        {
            AssertFailure(new QueryPlanValidator().Validate(Plan() with { Traversal = [path] }), code);
        }

        [Fact]
        public void Validate_CollectionTraversal_ReturnsExplicitCapabilityLimitation()
        {
            AssertFailure(new QueryPlanValidator().Validate(Plan() with { Entity = "contracts", Traversal = ["systemUsages"] }),
                "unsupported_relationship", "traversal[0]");
        }

        [Theory]
        [InlineData("name", "contains", "x")]
        [InlineData("name", "eq", "")]
        [InlineData("uuid", "eq", "7c3542f2-f39c-4c43-ac47-b9063054333b")]
        public void Validate_SupportedTextOrUuidFilter_PreservesValue(string field, string operation, string value)
        {
            var result = new QueryPlanValidator().Validate(Plan() with { Filters = [Filter(field, operation, value)] });
            Assert.True(result.Ok);
            Assert.Equal(value, result.Value.Filters.Single().Value.GetString());
        }

        [Theory]
        [InlineData("uuid", "contains", "x", "unsupported_operator")]
        [InlineData("name", "execute", "x", "unsupported_operator")]
        [InlineData("name", "contains", "", "invalid_value")]
        [InlineData("uuid", "eq", "not-a-uuid", "invalid_value")]
        [InlineData("deactivated", "eq", "true", "invalid_value")]
        [InlineData("uuid", "isNull", "true", "unsupported_operator")]
        [InlineData("noField", "eq", "x", "unsupported_field")]
        public void Validate_IncompatibleFilter_ReturnsFailure(string field, string operation, string value, string code)
        {
            AssertFailure(new QueryPlanValidator().Validate(Plan() with { Filters = [Filter(field, operation, value)] }), code);
        }

        [Theory]
        [InlineData("Municipality", true)]
        [InlineData("CommunityOfInterest", true)]
        [InlineData("Company", true)]
        [InlineData("OtherPublicAuthority", true)]
        [InlineData("municipality", false)]
        [InlineData("Unknown", false)]
        public void Validate_OrganizationType_UsesVerifiedEnumValues(string value, bool valid)
        {
            var result = new QueryPlanValidator().Validate(Plan() with
            {
                Entity = "organizations",
                Filters = [Filter("organizationType", "eq", value)]
            });
            Assert.Equal(valid, result.Ok);
        }

        [Theory]
        [InlineData("2026-10-09T10:00:00Z", true)]
        [InlineData("2026-10-09T10:00:00.1234567+02:00", true)]
        [InlineData("2026-10-09", false)]
        [InlineData("2026-10-09T10:00:00", false)]
        [InlineData("2026-02-31T10:00:00Z", false)]
        public void Validate_Timestamp_RequiresValidDateAndExplicitOffset(string value, bool valid)
        {
            var result = new QueryPlanValidator().Validate(Plan() with
            {
                Entity = "usages",
                Projection = ["uuid"],
                Filters = [Filter("lastModified", "before", value)]
            });
            Assert.Equal(valid, result.Ok);
        }

        [Fact]
        public void Validate_BooleanAndExplicitNullTests_RequireBooleanValues()
        {
            var validator = new QueryPlanValidator();
            Assert.True(validator.Validate(Plan() with { Filters = [Filter("deactivated", "eq", true)] }).Ok);
            Assert.True(validator.Validate(Plan() with { Entity = "organizations", Filters = [Filter("cvr", "isNull", true)] }).Ok);
            AssertFailure(validator.Validate(Plan() with { Entity = "organizations", Filters = [Filter("cvr", "eq", (object?)null)] }), "invalid_value");
            AssertFailure(validator.Validate(Plan() with { Entity = "organizations", Filters = [Filter("organizationType", "eq", 1)] }), "invalid_value");
            AssertFailure(validator.Validate(Plan() with { Filters = [new QueryFilter("name", "eq", default)] }), "invalid_value");
        }

        [Fact]
        public void Validate_ExcessiveCountsAndValueLengths_ReturnsLimits()
        {
            var validator = new QueryPlanValidator();
            AssertFailure(validator.Validate(Plan() with { Projection = [.. Enumerable.Repeat("uuid", QueryValidationLimits.MaxProjectionFields + 1)] }), "projection_limit");
            AssertFailure(validator.Validate(Plan() with { Filters = [.. Enumerable.Repeat(Filter("name", "eq", "a"), QueryValidationLimits.MaxFilters + 1)] }), "filter_limit");
            AssertFailure(validator.Validate(Plan() with { Traversal = [.. Enumerable.Repeat("parentSystem", QueryValidationLimits.MaxTraversals + 1)] }), "traversal_limit");
            AssertFailure(validator.Validate(Plan() with { Filters = [Filter("name", "eq", new string('a', QueryValidationLimits.MaxTextValueLength + 1))] }), "invalid_value");
            Assert.True(validator.Validate(Plan() with { Filters = [.. Enumerable.Repeat(Filter("name", "eq", "a"), QueryValidationLimits.MaxFilters)] }).Ok);
        }

        [Fact]
        public void Validate_DefaultArraysAndNullFilter_ReturnsFailure()
        {
            var validator = new QueryPlanValidator();
            AssertFailure(validator.Validate(Plan() with { Projection = default }), "projection_limit");
            AssertFailure(validator.Validate(Plan() with { Filters = default }), "filter_limit");
            AssertFailure(validator.Validate(Plan() with { Traversal = default }), "traversal_limit");
            AssertFailure(validator.Validate(Plan() with { Filters = [null!] }), "invalid_filter");
        }

        [Fact]
        public void Validate_DuplicatePaths_ReturnsFailure()
        {
            var validator = new QueryPlanValidator();
            AssertFailure(validator.Validate(Plan() with { Projection = ["uuid", "uuid"] }), "duplicate_projection");
            AssertFailure(validator.Validate(Plan() with { Traversal = ["parentSystem", "parentSystem"] }), "duplicate_traversal");
        }

        [Fact]
        public void Validate_ValidatedPlan_PreservesValuesAfterSourceReplacementAndDocumentDisposal()
        {
            var document = JsonDocument.Parse("\"original\"");
            var plan = Plan() with { Filters = [new QueryFilter("name", "eq", document.RootElement)] };
            var result = new QueryPlanValidator().Validate(plan);
            Assert.True(result.Ok);
            plan = plan with { Entity = "unknown", Filters = [Filter("name", "eq", "changed")] };
            document.Dispose();
            Assert.Equal("systems", result.Value.Entity);
            Assert.Equal("original", result.Value.Filters.Single().Value.GetString());
            Assert.Equal("unknown", plan.Entity);
        }

        [Fact]
        public void Validate_DisposedJsonValue_ReturnsInvalidValueInsteadOfThrowing()
        {
            var document = JsonDocument.Parse("\"original\"");
            var plan = Plan() with { Filters = [new QueryFilter("name", "eq", document.RootElement)] };
            document.Dispose();
            AssertFailure(new QueryPlanValidator().Validate(plan), "invalid_value", "filters[0].value");
        }

        private static QueryPlan Plan() => new("1.0.0", "systems", ["uuid", "name"], [], []);
        private static QueryFilter Filter(string field, string operation, object? value) => new(field, operation, JsonSerializer.SerializeToElement(value));
        private static string Serialize(QueryPlan plan) => JsonSerializer.Serialize(plan, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        private static void AssertFailure(Result<ValidatedQueryPlan, OperationError> result, string code, string? path = null)
        {
            Assert.True(result.Failed);
            var error = Assert.IsType<PlanValidationError>(result.Error);
            Assert.Equal(OperationFailure.BadInput, error.FailureType);
            Assert.Equal(code, error.Code);
            if (path is not null) Assert.Equal(path, error.Path);
        }
    }
}
