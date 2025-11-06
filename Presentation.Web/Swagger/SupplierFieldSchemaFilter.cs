using System;
using System.Linq;
using System.Reflection;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger;

namespace Presentation.Web.Swagger
{
	public class SupplierFieldSchemaFilter: ISchemaFilter
	{
        public void Apply(Schema schema, SchemaRegistry schemaRegistry, Type type)
        {
            if (schema?.properties == null) return;

            foreach (var property in type.GetProperties())
            {
                var attr = property.GetCustomAttribute<SupplierFieldAttribute>();
                if (attr == null) continue;

                var schemaProp = schema.properties.FirstOrDefault(p => string.Equals(p.Key, property.Name, StringComparison.OrdinalIgnoreCase));

                if (schemaProp.Key != null)
                {
                    var description = schemaProp.Value.description ?? string.Empty;
                    schemaProp.Value.description = $"{description} (Supplier Field)";
                }
            }
        }
    }
}