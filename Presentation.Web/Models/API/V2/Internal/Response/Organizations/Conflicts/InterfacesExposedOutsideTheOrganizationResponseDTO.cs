﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Presentation.Web.Models.API.V2.Internal.Response.Organizations.Conflicts
{
    public class InterfacesExposedOutsideTheOrganizationResponseDTO
    {
        public string ExposedIntefaceName { get; set; }
        public string ExposingSystemName { get; set; }
        public string OrganizationName { get; set; }
    }
}