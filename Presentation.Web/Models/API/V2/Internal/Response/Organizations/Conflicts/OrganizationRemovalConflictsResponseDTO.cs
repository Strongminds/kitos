using Presentation.Web.Models.API.V1.Organizations;
using Presentation.Web.Models.API.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Presentation.Web.Models.API.V2.Response.Generic.Identity;
using Presentation.Web.Models.API.V2.Internal.Response.Organizations.Conflicts;

namespace Presentation.Web.Models.API.V2.Internal.Response.Organizations
{
    public class OrganizationRemovalConflictsResponseDTO
    {
        public IEnumerable<MultipleConflictsResponseDTO> SystemsWithUsagesOutsideTheOrganization { get; set; }
        public IEnumerable<MultipleConflictsResponseDTO> InterfacesExposedOnSystemsOutsideTheOrganization { get; set; }
        public IEnumerable<MultipleConflictsResponseDTO> SystemsExposingInterfacesDefinedInOtherOrganizations { get; set; }
        public IEnumerable<MultipleConflictsResponseDTO> SystemsSetAsParentSystemToSystemsInOtherOrganizations { get; set; }
        public IEnumerable<SimpleConflictResponseDTO> DprInOtherOrganizationsWhereOrgIsDataProcessor { get; set; }
        public IEnumerable<SimpleConflictResponseDTO> DprInOtherOrganizationsWhereOrgIsSubDataProcessor { get; set; }
        public IEnumerable<SimpleConflictResponseDTO> ContractsInOtherOrganizationsWhereOrgIsSupplier { get; set; }
        public IEnumerable<SimpleConflictResponseDTO> SystemsInOtherOrganizationsWhereOrgIsRightsHolder { get; set; }
        public IEnumerable<SimpleConflictResponseDTO> SystemsWhereOrgIsArchiveSupplier { get; set; }
    }
}