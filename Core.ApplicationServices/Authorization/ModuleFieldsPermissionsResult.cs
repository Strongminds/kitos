using System.Collections.Generic;

namespace Core.ApplicationServices.Authorization
{
    public class ModuleFieldsPermissionsResult
    {
        public IEnumerable<FieldPermissionsResult> Fields { get; set; }

        public static ModuleFieldsPermissionsResult Create(IEnumerable<FieldPermissionsResult> fields)
        {
            return new ModuleFieldsPermissionsResult
            {
                Fields = fields
            };
        }
    }
}
