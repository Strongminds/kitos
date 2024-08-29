using System.Collections.Generic;
using Presentation.Web.Models.API.V2.Response.Generic.Identity;
using Presentation.Web.Models.API.V2.Types.Organization;

namespace Presentation.Web.Models.API.V2.Internal.Response.OrganizationUnit
{
    public class HierarchyOrganizationUnitResponseDTO : IdentityNamePairResponseDTO
    {
        public IEnumerable<HierarchyOrganizationUnitResponseDTO> Children { get; set; }
        public OrganizationUnitOriginChoice Origin { get; set; }    
    }
}