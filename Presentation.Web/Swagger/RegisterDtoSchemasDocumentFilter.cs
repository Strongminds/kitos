using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.Web.Swagger
{
    /// <summary>
    /// Scans the Presentation.Web assembly for public DTO types in a given namespace prefix
    /// and registers them as component schemas so they appear in the Swagger "Models" section.
    /// This is needed because the V1 (and V2) controllers return untyped IActionResult without
    /// [ProducesResponseType] annotations, meaning Swashbuckle cannot infer schemas from operations.
    /// </summary>
    public class RegisterDtoSchemasDocumentFilter : IDocumentFilter
    {
        private readonly Predicate<OpenApiDocument> _applyTo;
        private readonly string _namespacePrefix;

        public RegisterDtoSchemasDocumentFilter(Predicate<OpenApiDocument> applyTo, string namespacePrefix)
        {
            _applyTo = applyTo;
            _namespacePrefix = namespacePrefix;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (!_applyTo(swaggerDoc)) return;

            if (swaggerDoc.Components?.Schemas != null)
                ClearStaleCacheEntries(context.SchemaRepository, swaggerDoc.Components.Schemas);

            var assembly = typeof(RegisterDtoSchemasDocumentFilter).Assembly;

            var dtoTypes = assembly.GetTypes()
                .Where(t =>
                    t.Namespace != null &&
                    t.Namespace.StartsWith(_namespacePrefix, StringComparison.Ordinal) &&
                    t.IsPublic &&
                    (t.IsClass || t.IsEnum) &&
                    !t.IsAbstract &&
                    !t.IsGenericTypeDefinition &&
                    !t.IsNested);

            foreach (var type in dtoTypes)
            {
                try
                {
                    context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                }
                catch
                {
                    // Ignore types that cannot be represented as a schema (e.g. circular refs with no resolution)
                }
            }
        }

        /// <summary>
        /// Removes stale entries from SchemaRepository's internal type-to-schemaId cache.
        /// When PurgeUnusedTypesDocumentFilter removes schemas from Components.Schemas, the private
        /// _reservedIds cache still retains those types. This causes GenerateSchema to return a broken
        /// $ref (cache hit) without re-adding the schema, resulting in missing component schemas.
        /// Clearing stale entries forces re-generation of any purged schemas.
        /// </summary>
        private static void ClearStaleCacheEntries(SchemaRepository repo, IDictionary<string, IOpenApiSchema> presentSchemas)
        {
            var field = typeof(SchemaRepository).GetField("_reservedIds", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(repo) is not Dictionary<Type, string> reservedIds) return;

            var stale = reservedIds.Where(kv => !presentSchemas.ContainsKey(kv.Value)).Select(kv => kv.Key).ToList();
            foreach (var t in stale)
                reservedIds.Remove(t);
        }
    }
}
