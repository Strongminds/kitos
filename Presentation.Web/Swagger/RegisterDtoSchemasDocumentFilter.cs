using System;
using System.Linq;
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

            var assembly = typeof(RegisterDtoSchemasDocumentFilter).Assembly;

            var dtoTypes = assembly.GetTypes()
                .Where(t =>
                    t.Namespace != null &&
                    t.Namespace.StartsWith(_namespacePrefix, StringComparison.Ordinal) &&
                    t.IsPublic &&
                    t.IsClass &&
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
    }
}
