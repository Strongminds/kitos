﻿using System;
using System.ComponentModel.DataAnnotations;
using Presentation.Web.Models.API.V2.Types.Organization;

namespace Presentation.Web.Models.API.V2.Response.Organization
{
    public class OrganizationResponseDTO: ShallowOrganizationResponseDTO
    {
        [Required]
        public OrganizationType OrganizationType { get; }
        public bool IsSupplier { get; }
        public OrganizationResponseDTO(Guid uuid, string name, string cvr, OrganizationType organizationType, bool isSupplier) : base(uuid, name, cvr)
        {
            OrganizationType = organizationType;
            IsSupplier = isSupplier;
        }
    }
}