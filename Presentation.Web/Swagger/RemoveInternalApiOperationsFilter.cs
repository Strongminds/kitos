using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.Web.Swagger
{
    public class RemoveInternalApiOperationsFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var apiDescription in context.ApiDescriptions)
            {
                if (IsActionInternal(apiDescription))
                {
                    var route = "/" + apiDescription.RelativePath?.TrimEnd('/');
                    swaggerDoc.Paths.Remove(route);
                }
                else if (IsControllerInternal(apiDescription))
                {
                    var route = Regex.Replace("/" + apiDescription.RelativePath?.TrimEnd('/'), @"(\?.*)", "");
                    swaggerDoc.Paths.Remove(route);
                }
            }
        }

        private static bool IsActionInternal(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription apiDescription)
        {
            if (apiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
                return actionDescriptor.MethodInfo.GetCustomAttributes(typeof(InternalApiAttribute), true).Any();
            return apiDescription.ActionDescriptor.EndpointMetadata.OfType<InternalApiAttribute>().Any();
        }

        private static bool IsControllerInternal(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription apiDescription)
        {
            if (apiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
                return actionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(InternalApiAttribute), true).Any();
            return false;
        }
    }
}
