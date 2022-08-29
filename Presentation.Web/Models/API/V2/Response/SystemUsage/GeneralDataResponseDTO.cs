﻿using Presentation.Web.Models.API.V2.Response.Generic.Identity;
using Presentation.Web.Models.API.V2.Response.Generic.Validity;
using Presentation.Web.Models.API.V2.Types.SystemUsage;

namespace Presentation.Web.Models.API.V2.Response.SystemUsage
{
    public class GeneralDataResponseDTO
    {
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
        public ExpectedUsersIntervalDTO NumberOfExpectedUsers { get; set; }
        /// <summary>
        /// Specifies the validity of this system usage
        /// </summary>
        public ValidityResponseDTO Validity { get; set; }
        /// <summary>
        /// Defines the master contract for this system (many contracts can point to a system usage but only one can be the master contract)
        /// </summary>
        public IdentityNamePairResponseDTO MainContract { get; set; }
    }
}