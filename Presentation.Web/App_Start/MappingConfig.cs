using System;
using System.Linq;
using AutoMapper;
using Core.ApplicationServices;
using Core.DomainModel;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage.GDPR;
using Presentation.Web.Extensions;
using Presentation.Web.Infrastructure;
using Presentation.Web.Models.API.V1;
using Presentation.Web.Models.API.V1.ItSystemUsage;
using Advice = Core.DomainModel.Advice.Advice;
using DataRow = Core.DomainModel.ItSystem.DataRow;
using Organization = Core.DomainModel.Organization.Organization;

namespace Presentation.Web
{
    public class MappingConfig
    {
        public static IMapper CreateMapper()
        {
            var mappingconfig = new MapperConfiguration(configure: cfg =>
            {
                cfg.AllowNullDestinationValues = true;
                cfg.AddProfile<MappingProfile>();
                cfg.AddProfile<DropdownProfile>();
            });

            //Precompile automapper mappings to take performance hit during application boot in stead of during first request.
            mappingconfig.CompileMappings();

            return mappingconfig.CreateMapper();
        }
    }

    public class DropdownProfile : Profile
    {
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDTO>()
                  .ForMember(dest => dest.DefaultOrganizationUnitId,
                      opt => opt.MapFrom(src => src.OrganizationRights.FirstOrDefault() != null ? src.OrganizationRights.First().DefaultOrgUnitId : null))
                  .ForMember(dest => dest.DefaultOrganizationUnitUuid,
                      opt => opt.MapFrom(src => src.OrganizationRights.FirstOrDefault() != null ? src.OrganizationRights.First().DefaultOrgUnit.Uuid : (Guid?)null))
                  .ForMember(dest => dest.DefaultOrganizationUnitName,
                      opt => opt.MapFrom(src => src.OrganizationRights.FirstOrDefault() != null ? src.OrganizationRights.First().DefaultOrgUnit.Name : null))
                  .ReverseMap()
                  .IgnoreDestinationEntityFields();

            CreateMap<Organization, OrganizationDTO>()
                .ForMember(dest => dest.Root, opt => opt.MapFrom(src => src.GetRoot()))
                .ReverseMap()
                .IgnoreDestinationEntityFields();

            CreateMap<Organization, OrganizationSimpleDTO>();

            CreateMap<OrganizationUnit, OrgUnitDTO>()
                  .ForMember(dest => dest.Children, opt => opt.MapFrom(unit => unit.Children.OrderBy(child => child.Name, KitosConstants.DanishStringComparer).ToList()))
                  .ReverseMap()
                  .ForMember(dest => dest.Children, opt => opt.Ignore())
                  .IgnoreDestinationEntityFields();

            CreateMap<OrganizationUnit, OrgUnitSimpleDTO>();

            CreateMap<TaskRef, TaskRefDTO>()
                  .ReverseMap()
                  .IgnoreDestinationEntityFields();

            CreateMap<OrganizationRight, OrganizationRightDTO>()
                .ForMember(dest => dest.OrganizationUuid, opt => opt.MapFrom(src => src.Organization.Uuid))
                .ReverseMap()
                .IgnoreDestinationEntityFields();

            CreateMap<RightInputDTO, OrganizationUnitRight>();

            CreateMap<RightInputDTO, ItSystemRight>();

            CreateMap<RightInputDTO, ItContractRight>();

            CreateMap<DataRow, DataRowDTO>()
                  .ReverseMap()
                  .IgnoreDestinationEntityFields();

            CreateMap<ItSystem, ItSystemDTO>()
                .ForMember(dest => dest.TaskRefIds, opt => opt.MapFrom(src => src.TaskRefs.Select(x => x.Id)))
                .ForMember(dest => dest.IsUsed, opt => opt.MapFrom(src => src.Usages.Any()))
                .ReverseMap()
                .ForMember(dest => dest.TaskRefs, opt => opt.Ignore())
                .ForMember(dest => dest.ItInterfaceExhibits, opt => opt.Ignore())
                .IgnoreDestinationEntityFields();

            //Output only - this mapping should not be reversed
            CreateMap<ItContract, ItContractSystemDTO>()
                .ForMember(dest => dest.AgreementElements, opt => opt.MapFrom(src => src.AssociatedAgreementElementTypes.Select(x => x.AgreementElementType)));

            CreateMap<Advice, AdviceDTO>()
                  .ReverseMap()
                  .IgnoreDestinationEntityFields();

            //Output only - this mapping should not be reversed
            CreateMap<ExcelImportError, ExcelImportErrorDTO>();

            CreateMap<ItSystemUsageSensitiveDataLevel, ItSystemUsageSensitiveDataLevelDTO>()
                .ForMember(dest => dest.DataSensitivityLevel, opt => opt.MapFrom(src => src.SensitivityDataLevel));

            //DPR
            CreateMap<DataProcessingRegistration, NamedEntityDTO>();
        }
    }
}
