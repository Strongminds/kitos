﻿using System;
using Presentation.Web.Models.API.V2.Internal.Response.Organizations;

namespace Presentation.Web.Models.API.V2.Response.Organization
{
    public class OrganizationMasterDataRolesResponseDTO
    {
        public Guid OrganizationUuid { get; set; }
        public ContactPersonResponseDTO ContactPerson { get; set; }
        public DataResponsibleResponseDTO DataResponsible { get; set; }
        public DataProtectionAdvisorResponseDTO DataProtectionAdvisor { get; set; }
    }
}