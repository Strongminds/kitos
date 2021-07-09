﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Web.Models.External.V2.Response.SystemUsage
{
    /// <summary>
    /// NOTE: IT-System usages are registrations which extend those of a system within the context of a specific organization.
    /// They don't have their own identity ensuring identity-stability across use/discard/use scenarios.
    /// </summary>
    public class ItSystemUsageResponseDTO
    {
        /// <summary>
        /// IT-System which this organizational usage is based on
        /// </summary>
        [Required]
        public IdentityNamePairResponseDTO SystemContext { get; set; }
        /// <summary>
        /// Organization in which the system usage has been created
        /// </summary>
        [Required]
        public ShallowOrganizationDTO OrganizationContext { get; set; }
        /// <summary>
        /// System Id assigned locally within the organization
        /// </summary>
        public string LocalSystemId { get; set; }
        /// <summary>
        /// Call name used locally within the organization
        /// </summary>
        public string LocalCallName { get; set; }
        /// <summary>
        /// Optional classification of the registered data
        /// </summary>
        public IdentityNamePairResponseDTO DataClassification { get; set; }
        /// <summary>
        /// Notes relevant to the system usage within the organization
        /// </summary>
        public string Notes { get; set; }
        /// <summary>
        /// Locally registered system version
        /// </summary>
        public string SystemVersion { get; set; }
        /// <summary>
        /// Interval which defines the number of expected users this system has within the organization
        /// </summary>
        public ExpectedUsersIntervalResponseDTO NumberOfExpectedUsers { get; set; }
        /// <summary>
        /// Specifies the validity of this system usage
        /// </summary>
        public ItSystemUsageValidityResponseDTO Validity { get; set; }
        /// <summary>
        /// Defines the master contract for this system (many contracts can point to a system usage but only one can be the master contract)
        /// </summary>
        public IdentityNamePairResponseDTO MainContract { get; set; }
        /// <summary>
        /// A collection of IT-System usage role option assignments
        /// </summary>
        public IEnumerable<RoleAssignmentResponseDTO> Roles { get; set; }
        /// <summary>
        /// A collection of organization units which have taken this system into use
        /// </summary>
        public IEnumerable<IdentityNamePairResponseDTO> OrganizationUnitsUsingThisSystem { get; set; }
        /// <summary>
        /// Out of all of the using organization units, this one is responsible for the system within the organization.
        /// </summary>
        public IdentityNamePairResponseDTO ResponsibleOrganizationUnit { get; set; }
        /// <summary>
        /// Defines IT-System KLE deviations locally within an organization. All deviations are in the context of the inherited deviations which are found on the IT-System context
        /// </summary>
        public LocalKLEDeviations LocalKLEDeviations { get; set; }
        /// <summary>
        /// IT-Projects associated with this system usage
        /// </summary>
        public IEnumerable<IdentityNamePairResponseDTO> AssociatedProjects { get; set; }
        /// <summary>
        /// User defined external references
        /// </summary>
        public IEnumerable<ExternalReferenceResponseDTO> ExternalReferences { get; set; }

    }
}