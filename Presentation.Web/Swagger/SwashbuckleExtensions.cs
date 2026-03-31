using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Presentation.Web.Swagger
{
    public static class SwashbuckleExtensions
    {
        public static IEnumerable<OpenApiOperation> EnumerateOperations(this OpenApiPathItem pathItem)
        {
            if (pathItem == null) yield break;
            foreach (var operation in pathItem.Operations.Values)
                yield return operation;
        }

        public static IEnumerable<OpenApiSchema> EnumerateSchemas(this OpenApiOperation operation)
        {
            if (operation == null) yield break;

            foreach (var response in operation.Responses ?? new OpenApiResponses())
            {
                foreach (var content in response.Value.Content?.Values ?? Enumerable.Empty<OpenApiMediaType>())
                {
                    if (content.Schema != null) yield return content.Schema;
                    if (content.Schema?.Items != null) yield return content.Schema.Items;
                }
            }

            foreach (var parameter in operation.Parameters ?? new List<OpenApiParameter>())
            {
                if (parameter.Schema != null) yield return parameter.Schema;
            }

            if (operation.RequestBody != null)
            {
                foreach (var content in operation.RequestBody.Content?.Values ?? Enumerable.Empty<OpenApiMediaType>())
                {
                    if (content.Schema != null) yield return content.Schema;
                }
            }
        }

        public static IEnumerable<OpenApiSchema> StartSchemaEnumeration(this OpenApiSchema schema, IDictionary<string, OpenApiSchema> schemaByTypeName)
        {
            return schema.EnumerateSchemaRecursive(schemaByTypeName, new HashSet<string>());
        }

        private static IEnumerable<OpenApiSchema> EnumerateSchemaRecursive(this OpenApiSchema schema, IDictionary<string, OpenApiSchema> schemaByTypeName, ISet<string> visitedDefinitions, bool isRoot = true)
        {
            var root = schema?.GetRootSchemaOrNull();
            if (root?.Reference?.Id == null) yield break;

            if (isRoot)
            {
                visitedDefinitions.Add(root.Reference.Id);
                yield return schema;
            }

            foreach (var referencedSchema in schema.FindReferencedSchemas(schemaByTypeName).ToList())
            {
                var refId = referencedSchema.Reference?.Id;
                if (refId == null || !visitedDefinitions.Add(refId)) continue;

                yield return referencedSchema;

                foreach (var childSchema in referencedSchema.EnumerateSchemaRecursive(schemaByTypeName, visitedDefinitions, false).ToList())
                {
                    var childRefId = childSchema.GetRootSchemaOrNull()?.Reference?.Id;
                    if (childRefId != null && visitedDefinitions.Add(childRefId))
                        yield return childSchema;
                }
            }
        }

        private static IEnumerable<OpenApiSchema> FindReferencedSchemas(this OpenApiSchema schema, IDictionary<string, OpenApiSchema> schemaByTypeName)
        {
            var key = schema.GetSchemaTypeKey();
            if (string.IsNullOrEmpty(key) || !schemaByTypeName.TryGetValue(key, out var definition))
                return Enumerable.Empty<OpenApiSchema>();

            return definition.Properties.Values
                .Select(p => p.GetRootSchemaOrNull())
                .Where(s => s != null);
        }

        public static string GetSchemaRefOrNull(this OpenApiSchema schema) =>
            GetRootSchemaOrNull(schema)?.Reference?.Id;

        public static string GetSchemaTypeKey(this OpenApiSchema schema) =>
            GetSchemaRefOrNull(schema);

        public static OpenApiSchema GetRootSchemaOrNull(this OpenApiSchema schema)
        {
            if (schema?.Reference != null) return schema;
            if (schema?.Items?.Reference != null) return schema.Items;
            return null;
        }
    }
}
