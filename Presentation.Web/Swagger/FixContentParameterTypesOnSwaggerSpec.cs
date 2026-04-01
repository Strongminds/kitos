using System.Collections.Generic;
using Microsoft.OpenApi;
using Presentation.Web.Helpers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.Web.Swagger
{
    public class FixContentParameterTypesOnSwaggerSpec : IOperationFilter
    {
        private const string Json = "application/json";
        private const string JsonMergePatch = "application/merge-patch+json"; // https://datatracker.ietf.org/doc/html/rfc7396

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Remove merge-patch from request body content
            operation.RequestBody?.Content?.Remove(JsonMergePatch);

            // Remove merge-patch from all response content
            foreach (var response in operation.Responses.Values)
            {
                response.Content?.Remove(JsonMergePatch);
            }

            var relativePath = context.ApiDescription.RelativePath ?? string.Empty;
            if (!relativePath.IsExternalApiPath())
                return;

            // Clear and set explicit content types for external API paths
            if (operation.RequestBody != null)
                operation.RequestBody.Content.Clear();

            foreach (var response in operation.Responses.Values)
                response.Content?.Clear();

            var httpMethod = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;
            switch (httpMethod)
            {
                case "POST":
                case "PUT":
                    EnsureRequestBodyContent(operation, Json);
                    EnsureResponseContent(operation, Json);
                    break;
                case "PATCH":
                    EnsureRequestBodyContent(operation, JsonMergePatch);
                    EnsureRequestBodyContent(operation, Json);
                    EnsureResponseContent(operation, Json);
                    break;
                case "GET":
                    EnsureResponseContent(operation, Json);
                    break;
                case "DELETE":
                default:
                    break;
            }
        }

        private static void EnsureRequestBodyContent(OpenApiOperation operation, string mediaType)
        {
            if (operation.RequestBody == null)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>()
                };
            }
            operation.RequestBody.Content.TryAdd(mediaType, new OpenApiMediaType());
        }

        private static void EnsureResponseContent(OpenApiOperation operation, string mediaType)
        {
            foreach (var response in operation.Responses.Values)
            {
                if (response.Content == null) continue;
                response.Content.TryAdd(mediaType, new OpenApiMediaType());
            }
        }
    }
}
