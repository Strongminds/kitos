﻿using System;
using Core.DomainModel.Organization;

namespace Presentation.Web.Models.API.V1.Organizations
{
    public class ConnectionUpdateOrganizationUnitConsequenceDTO
    {
        public Guid Uuid { get; set; }
        public string Name { get; set; }
        public ConnectionUpdateOrganizationUnitChangeType Category { get; set; }
        public string Description { get; set; }
    }
}