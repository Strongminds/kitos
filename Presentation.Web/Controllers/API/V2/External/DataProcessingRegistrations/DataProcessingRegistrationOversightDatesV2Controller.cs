using Core.ApplicationServices.GDPR.Write;
using Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Response.DataProcessing;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Net;
using System.Web.Http;
using Presentation.Web.Models.API.V2.Types.DataProcessing;

namespace Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations
{
    /// <summary>
    /// API for the data processing registrations in KITOS
    /// </summary>
    [RoutePrefix("api/v2/data-processing-registrations/{uuid}/oversight-dates")]
    public class DataProcessingRegistrationOversightDatesV2Controller : ExternalBaseController
    {

        private readonly IDataProcessingRegistrationWriteService _writeService;
        private readonly IDataProcessingRegistrationWriteModelMapper _writeModelMapper;
        private readonly IDataProcessingRegistrationResponseMapper _responseMapper;

        public DataProcessingRegistrationOversightDatesV2Controller(IDataProcessingRegistrationWriteService writeService,
            IDataProcessingRegistrationWriteModelMapper writeModelMapper,
            IDataProcessingRegistrationResponseMapper responseMapper)
        {
            _writeService = writeService;
            _writeModelMapper = writeModelMapper;
            _responseMapper = responseMapper;
        }

        /// <summary>
        /// Add an oversight date to an existing data processing registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OversightDateDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PostDataProcessingRegistrationOversightDate([NonEmptyGuid] Guid uuid, [FromBody] ModifyOversightDateDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .AddOversight(uuid, _writeModelMapper.FromOversightPOST(request))
                .Select(_responseMapper.MapOversightDate)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Partially updates an existing oversight date in a data processing registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{oversightDateUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OversightDateDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PatchDataProcessingRegistrationOversightDate([NonEmptyGuid] Guid uuid, [NonEmptyGuid] Guid oversightDateUuid, [FromBody] ModifyOversightDateDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .UpdateOversight(uuid, oversightDateUuid, _writeModelMapper.FromOversightPATCH(request))
                .Select(_responseMapper.MapOversightDate)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Deletes an oversight date from a data processing registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{oversightDateUuid}")]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult DeleteDataProcessingRegistrationOversightDate([NonEmptyGuid] Guid uuid, [NonEmptyGuid] Guid oversightDateUuid, [FromBody] ModifyOversightDateDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .DeleteOversight(uuid, oversightDateUuid)
                .Match(FromOperationError, NoContent);
        }
    }
}