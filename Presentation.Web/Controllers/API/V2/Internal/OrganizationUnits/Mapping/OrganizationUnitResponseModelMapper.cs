using System.Linq;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Models.API.V2.Internal.Response.OrganizationUnit;

namespace Presentation.Web.Controllers.API.V2.Internal.OrganizationUnits.Mapping
{
    public class OrganizationUnitResponseModelMapper : IOrganizationUnitResponseModelMapper
    {
        public OrganizationUnitResponseDTO ToUnitDto(OrganizationUnit unit)
        {
            return new OrganizationUnitResponseDTO
            {
                Uuid = unit.Uuid,
                Name = unit.Name,
                Ean = unit.Ean,
                Origin = unit.Origin.ToOrganizationUnitOriginChoice(),
                ParentOrganizationUnit = unit.Parent?.MapIdentityNamePairDTO()
            };
        }

        public HierarchyOrganizationUnitResponseDTO ToHierarchyUnitDto(OrganizationUnit root)
        {
            return new HierarchyOrganizationUnitResponseDTO
            {
                Uuid = root.Uuid,
                Name = root.Name,
                Origin = root.Origin.ToOrganizationUnitOriginChoice(),
                Children = root.Children.Select(ToHierarchyUnitDto).ToList()
            };
        }
    }
}