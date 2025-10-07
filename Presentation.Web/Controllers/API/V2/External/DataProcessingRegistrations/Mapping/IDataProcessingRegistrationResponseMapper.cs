using Core.ApplicationServices.Model.GDPR;
using Core.DomainModel.GDPR;
using Presentation.Web.Models.API.V2.Response.DataProcessing;
using Presentation.Web.Models.API.V2.Types.DataProcessing;

namespace Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations.Mapping
{
    public interface IDataProcessingRegistrationResponseMapper
    {
        DataProcessingRegistrationResponseDTO MapDataProcessingRegistrationDTO(DataProcessingRegistration dataProcessingRegistration);

        DataProcessingRegistrationPermissionsResponseDTO MapPermissions(
            DataProcessingRegistrationPermissions permissions);

        OversightDateDTO MapOversightDate(DataProcessingRegistrationOversightDate oversightDate);
    }
}
