using System;

namespace Presentation.Web.Models.API.V2.Internal.Response.Organizations
{
    /// <summary>
    /// Status of the FK Organisation users connection. Independent of the org-unit connection.
    /// </summary>
    public class StsOrganizationUserSynchronizationDetailsResponseDTO
    {
        public required StsOrganizationAccessStatusResponseDTO AccessStatus { get; set; }
        /// <summary>
        /// Determines if KITOS accepts external user changes from FK Organisation for the organization
        /// </summary>
        public bool Connected { get; set; }
        public bool CanCreateConnection { get; set; }
        public bool CanDeleteConnection { get; set; }
        /// <summary>
        /// UTC time when the connection was created. Null if not connected.
        /// </summary>
        public DateTime? ConnectedAt { get; set; }
    }
}
