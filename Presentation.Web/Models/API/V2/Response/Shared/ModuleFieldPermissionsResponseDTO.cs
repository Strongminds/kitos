﻿using System.Collections.Generic;

namespace Presentation.Web.Models.API.V2.Response.Shared
{
    public class ModuleFieldPermissionsResponseDTO
    {
        public IEnumerable<FieldPermissionsResponseDTO> Fields { get; set; }
    }
}