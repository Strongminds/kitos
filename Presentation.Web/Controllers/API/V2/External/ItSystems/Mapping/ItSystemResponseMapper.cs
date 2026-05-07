using System.Linq;
using Core.Abstractions.Extensions;
using Core.ApplicationServices.Model.System;
using Core.DomainModel.ItSystem;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.External.Generic;
using Presentation.Web.Models.API.V2.Response.System;
using Presentation.Web.Models.API.V2.Types.Shared;
using Presentation.Web.Models.API.V2.Types.System;
using System;
using System.Collections.Generic;

namespace Presentation.Web.Controllers.API.V2.External.ItSystems.Mapping
{
    public class ItSystemResponseMapper : IItSystemResponseMapper
    {
        private readonly IExternalReferenceResponseMapper _referenceResponseMapper;

        public ItSystemResponseMapper(IExternalReferenceResponseMapper referenceResponseMapper)
        {
            _referenceResponseMapper = referenceResponseMapper;
        }

        public RightsHolderItSystemResponseDTO ToRightsHolderResponseDTO(ItSystem itSystem)
        {
            var dto = new RightsHolderItSystemResponseDTO();
            MapBaseInformation(itSystem, dto);
            return dto;
        }

        public ItSystemResponseDTO ToSystemResponseDTO(ItSystem itSystem)
        {
            var dto = new ItSystemResponseDTO
            {
                UsingOrganizations = itSystem
                    .Usages
                    .Select(systemUsage => systemUsage.Organization)
                    .Select(organization => organization.MapShallowOrganizationResponseDTO())
                    .ToList(),
                LastModified = itSystem.LastChanged,
                LastModifiedBy = itSystem.LastChangedByUser?.MapIdentityNamePairDTO(),
                Scope = itSystem.AccessModifier.ToChoice(),
                OrganizationContext = itSystem.Organization?.MapShallowOrganizationResponseDTO(),
                LegalName = itSystem.LegalName,
                LegalDataProcessorName = itSystem.LegalDataProcessorName
            };

            MapBaseInformation(itSystem, dto);

            return dto;
        }

        public ItSystemPermissionsResponseDTO MapPermissions(SystemPermissions permissions)
        {
            return new ItSystemPermissionsResponseDTO
            {
                Delete = permissions.BasePermissions.Delete,
                Modify = permissions.BasePermissions.Modify,
                Read = permissions.BasePermissions.Read,
                DeletionConflicts = permissions.DeletionConflicts.Select(MapConflict).ToList(),
                ModifyVisibility = permissions.ModifyVisibility
            };
        }

        private static IList<LicensingAndCodeModelChoice> MapLicensingAndCodeModels(IEnumerable<LicensingAndCodeModel> domainModels)
        {
            return domainModels.Select(domain => 
                 domain switch
                 {
                    LicensingAndCodeModel.OpenSource => LicensingAndCodeModelChoice.OpenSource,
                    LicensingAndCodeModel.Freeware => LicensingAndCodeModelChoice.Freeware,
                    LicensingAndCodeModel.Proprietary => LicensingAndCodeModelChoice.Proprietary,
                    _ => throw new ArgumentOutOfRangeException(nameof(domainModels), $"Invalid value provided for enum conversion: {domainModels}"),    
            }).ToList();
        }
        
        private static Models.API.V2.Types.System.SystemDeletionConflict MapConflict(Core.ApplicationServices.Model.System.SystemDeletionConflict arg)
        {
            return arg.ToChoice();
        }

        private void MapBaseInformation<T>(ItSystem itSystem, T dto) where T : BaseItSystemResponseDTO
        {
            dto.Uuid = itSystem.Uuid;
            dto.ExternalUuid = itSystem.ExternalUuid;
            dto.Name = itSystem.Name;
            dto.RightsHolder = itSystem.BelongsTo?.Transform(organization => organization.MapShallowOrganizationResponseDTO());
            dto.BusinessType = itSystem.BusinessType?.Transform(businessType => businessType.MapIdentityNamePairDTO());
            dto.Description = itSystem.Description;
            dto.CreatedBy = itSystem.ObjectOwner?.MapIdentityNamePairDTO();
            dto.Created = itSystem.Created;
            dto.Deactivated = itSystem.Disabled;
            dto.FormerName = itSystem.PreviousName;
            dto.ParentSystem = itSystem.Parent?.Transform(parent => parent.MapIdentityNamePairDTO());
            dto.ExternalReferences = _referenceResponseMapper.MapExternalReferences(itSystem.ExternalReferences).ToList();
            dto.RecommendedArchiveDuty = new RecommendedArchiveDutyResponseDTO(itSystem.ArchiveDutyComment, itSystem.ArchiveDuty?.ToChoice() ?? RecommendedArchiveDutyChoice.Undecided);
            dto.KLE = itSystem
                .TaskRefs
                .Select(taskRef => taskRef.MapIdentityNamePairDTO())
                .ToList();
            dto.MainContractSuppliers =
                itSystem.Usages.Select(x => x.MainContract?.ItContract.Supplier)
                    .Where(x => x != null)
                    .DistinctBy(x => x.Uuid)
                    .Select(x => x.MapShallowOrganizationResponseDTO())
                    .ToList();
            dto.LicensingAndCodeModels = MapLicensingAndCodeModels(itSystem.LicensingAndCodeModels);
        }
    }
}
