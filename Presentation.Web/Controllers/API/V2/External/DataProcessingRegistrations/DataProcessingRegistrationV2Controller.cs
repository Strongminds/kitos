﻿using Core.ApplicationServices.GDPR;
using Core.DomainModel.GDPR;
using Core.DomainServices.Queries;
using Core.DomainServices.Queries.DPR;
using Presentation.Web.Extensions;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;
using Core.Abstractions.Extensions;
using Core.ApplicationServices.GDPR.Write;
using Core.ApplicationServices.Model.GDPR.Write;
using Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations.Mapping;
using Presentation.Web.Models.API.V2.Request.DataProcessing;
using Presentation.Web.Models.API.V2.Request.Generic.Roles;
using Presentation.Web.Models.API.V2.Response.DataProcessing;
using Presentation.Web.Models.API.V2.Types.Shared;

namespace Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations
{
    /// <summary>
    /// API for the data processing registrations in KITOS
    /// </summary>
    [RoutePrefix("api/v2/data-processing-registrations")]
    public class DataProcessingRegistrationV2Controller : ExternalBaseController
    {
        private readonly IDataProcessingRegistrationApplicationService _dataProcessingRegistrationService;
        private readonly IDataProcessingRegistrationWriteService _writeService;
        private readonly IDataProcessingRegistrationWriteModelMapper _writeModelMapper;
        private readonly IDataProcessingRegistrationResponseMapper _responseMapper;

        public DataProcessingRegistrationV2Controller(
            IDataProcessingRegistrationApplicationService dataProcessingRegistrationService,
            IDataProcessingRegistrationWriteService writeService,
            IDataProcessingRegistrationWriteModelMapper writeModelMapper,
            IDataProcessingRegistrationResponseMapper responseMapper)
        {
            _dataProcessingRegistrationService = dataProcessingRegistrationService;
            _writeService = writeService;
            _writeModelMapper = writeModelMapper;
            _responseMapper = responseMapper;
        }

        /// <summary>
        /// Returns all Data-Processing-Registrations available to the user
        /// </summary>
        /// <param name="organizationUuid">Organization UUID filter</param>
        /// <param name="systemUuid">System UUID filter</param>
        /// <param name="systemUsageUuid">SystemUsage UUID filter</param>
        /// <param name="dataProcessorUuid">UUID of a data processor in the registration</param>
        /// <param name="subDataProcessorUuid">UUID of a sub data processor in the registration</param>
        /// <param name="agreementConcluded">Filter based on whether or not an agreement has been concluded</param>
        /// <returns></returns>
        [HttpGet]
        [Route]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<DataProcessingRegistrationResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult GetDataProcessingRegistrations(
            [NonEmptyGuid] Guid? organizationUuid = null,
            [NonEmptyGuid] Guid? systemUuid = null,
            [NonEmptyGuid] Guid? systemUsageUuid = null,
            [NonEmptyGuid] Guid? dataProcessorUuid = null,
            [NonEmptyGuid] Guid? subDataProcessorUuid = null,
            bool? agreementConcluded = null,
            [FromUri] BoundedPaginationQuery paginationQuery = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var conditions = new List<IDomainQuery<DataProcessingRegistration>>();

            if (organizationUuid.HasValue)
                conditions.Add(new QueryByOrganizationUuid<DataProcessingRegistration>(organizationUuid.Value));

            if (systemUuid.HasValue)
                conditions.Add(new QueryBySystemUuid(systemUuid.Value));

            if (systemUsageUuid.HasValue)
                conditions.Add(new QueryBySystemUsageUuid(systemUsageUuid.Value));

            if (dataProcessorUuid.HasValue)
                conditions.Add(new QueryByDataProcessorUuid(dataProcessorUuid.Value));

            if (subDataProcessorUuid.HasValue)
                conditions.Add(new QueryBySubDataProcessorUuid(subDataProcessorUuid.Value));

            if (agreementConcluded.HasValue)
                conditions.Add(new QueryByAgreementConcluded(agreementConcluded.Value));

            return _dataProcessingRegistrationService
                .Query(conditions.ToArray())
                .OrderBy(dpr => dpr.Id)
                .Page(paginationQuery)
                .ToList()
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Transform(Ok);
        }

        /// <summary>
        /// Returns a specific Data-Processing-Registration
        /// </summary>
        /// <param name="uuid">UUID of Data-Processing-Registration entity</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{uuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult GetDataProcessingRegistration([NonEmptyGuid] Guid uuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _dataProcessingRegistrationService
                .GetByUuid(uuid)
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Create a new data processing registration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route]
        [SwaggerResponse(HttpStatusCode.Created, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PostDataProcessingRegistration([FromBody] CreateDataProcessingRegistrationRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Create(request.OrganizationUuid, _writeModelMapper.FromPOST(request))
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(MapCreatedResponse, FromOperationError);
        }

        /// <summary>
        /// Perform a full update of an existing data processing registration.
        /// Note: PUT expects a full version of the updated registration. For partial updates, please use PATCH.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{uuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutDataProcessingRegistration([NonEmptyGuid] Guid uuid, [FromBody] UpdateDataProcessingRegistrationRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(uuid, _writeModelMapper.FromPUT(request))
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Allows partial updates of an existing data processing registration
        /// NOTE:At the root level, defined sections will be mapped as changes e.g. {General: null} will reset the entire "General" section.
        /// If the section is not provided in the json, the omitted section will remain unchanged.
        /// At the moment we only manage PATCH at the root level so all levels below that must be provided in it's entirety 
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{uuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PatchDataProcessingRegistration([NonEmptyGuid] Guid uuid, [FromBody] UpdateDataProcessingRegistrationRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(uuid, _writeModelMapper.FromPATCH(request))
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Removes an existing data processing registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{uuid}")]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult DeleteDataProcessingRegistration([NonEmptyGuid] Guid uuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Delete(uuid)
                .Match(FromOperationError, () => StatusCode(HttpStatusCode.NoContent));
        }

        /// <summary>
        /// Perform a full update of the "General data" section of an existing data processing registration.
        /// Absent/nulled fields will result in a data reset in the targeted registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{uuid}/general")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutDataProcessingRegistrationGeneralData([NonEmptyGuid] Guid uuid, [FromBody] DataProcessingRegistrationGeneralDataWriteRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(uuid, new DataProcessingRegistrationModificationParameters
                {
                    General = _writeModelMapper.MapGeneral(request)
                })
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Perform a full update of the "SystemUsages" section of an existing data processing registration.
        /// Absent/nulled fields will result in a data reset in the targeted registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="systemUsageUuids"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{uuid}/system-usages")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutDataProcessingRegistrationSystemsData([NonEmptyGuid] Guid uuid, [FromBody] IEnumerable<Guid> systemUsageUuids)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(uuid, new DataProcessingRegistrationModificationParameters
                {
                    SystemUsageUuids = systemUsageUuids.FromNullable()
                })
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Perform a full update of the "Oversight" section of an existing data processing registration.
        /// Absent/nulled fields will result in a data reset in the targeted registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{uuid}/oversight")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutDataProcessingRegistrationOversightData([NonEmptyGuid] Guid uuid, [FromBody] DataProcessingRegistrationOversightWriteRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(uuid, new DataProcessingRegistrationModificationParameters
                {
                    Oversight = _writeModelMapper.MapOversight(request)
                })
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Perform a full update of the "Roles" section of an existing data processing registration.
        /// Absent/nulled fields will result in a data reset in the targeted registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{uuid}/roles")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutDataProcessingRegistrationRolesData([NonEmptyGuid] Guid uuid, [FromBody] IEnumerable<RoleAssignmentRequestDTO> request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(uuid, new DataProcessingRegistrationModificationParameters
                {
                    Roles = _writeModelMapper.MapRoles(request)
                })
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Perform a full update of the "External references" section of an existing data processing registration.
        /// Absent/nulled fields will result in a data reset in the targeted registration.
        /// </summary>
        /// <param name="uuid">UUID of the data processing registration</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{uuid}/external-references")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(DataProcessingRegistrationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutDataProcessingRegistrationExternalReferencesData([NonEmptyGuid] Guid uuid, [FromBody] IEnumerable<ExternalReferenceDataDTO> request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(uuid, new DataProcessingRegistrationModificationParameters
                {
                    ExternalReferences = _writeModelMapper.MapReferences(request).FromNullable()
                })
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        private CreatedNegotiatedContentResult<DataProcessingRegistrationResponseDTO> MapCreatedResponse(DataProcessingRegistrationResponseDTO dto)
        {
            return Created($"{Request.RequestUri.AbsoluteUri.TrimEnd('/')}/{dto.Uuid}", dto);
        }
    }
}