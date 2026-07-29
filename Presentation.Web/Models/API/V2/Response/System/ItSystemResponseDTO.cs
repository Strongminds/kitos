using System;
using System.Collections.Generic;
using Presentation.Web.Models.API.V2.Response.Generic.Identity;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Models.API.V2.SharedProperties;
using Presentation.Web.Models.API.V2.Types.Shared;

namespace Presentation.Web.Models.API.V2.Response.System
{
    public class ItSystemResponseDTO : BaseItSystemResponseDTO, IHasLastModified, IHasRegistrationScope, IHasOrganizationContext
    {
        /// <summary>
        /// Organizations using this IT-System
        /// </summary>
        public required IEnumerable<ShallowOrganizationResponseDTO> UsingOrganizations { get; set; }

        /// <summary>
        /// UTC timestamp of latest modification
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Responsible for last modification
        /// </summary>
        public IdentityNamePairResponseDTO? LastModifiedBy { get; set; }
        /// <summary>
        /// Scope of the registration
        /// - Local: The scope of the registration is local to the organization in which is was created
        /// - Global: The scope of the registration is global to KITOS and can be accessed and associated by authorized clients
        /// </summary>
        public required RegistrationScopeChoice Scope { get; set; }
        /// <summary>
        /// Organization in which this it-system master data was created
        /// </summary>
        public ShallowOrganizationResponseDTO? OrganizationContext { get; set; }

        public string? LegalName { get; set; }
        public string? LegalDataProcessorName { get; set; }

    }
}