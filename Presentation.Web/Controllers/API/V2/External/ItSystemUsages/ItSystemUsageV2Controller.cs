﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;
using Core.ApplicationServices.Model.SystemUsage.Write;
using Core.ApplicationServices.SystemUsage;
using Core.ApplicationServices.SystemUsage.Write;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Result;
using Core.DomainServices.Queries;
using Core.DomainServices.Queries.SystemUsage;
using Infrastructure.Services.Types;
using Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping;
using Presentation.Web.Extensions;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Request.Generic.Roles;
using Presentation.Web.Models.API.V2.Response.SystemUsage;
using Presentation.Web.Models.API.V2.Types.Shared;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    /// <summary>
    /// API for the local registrations related to it-systems in KITOS
    /// NOTE: IT-System usages are registrations which extend those of a system within the context of a specific organization.
    /// </summary>
    [RoutePrefix("api/v2/it-system-usages")]
    public class ItSystemUsageV2Controller : ExternalBaseController
    {
        private readonly IItSystemUsageService _itSystemUsageService;
        private readonly IItSystemUsageResponseMapper _responseMapper;
        private readonly IItSystemUsageWriteService _writeService;
        private readonly IItSystemUsageWriteModelMapper _writeModelMapper;

        public ItSystemUsageV2Controller(
            IItSystemUsageService itSystemUsageService,
            IItSystemUsageResponseMapper responseMapper,
            IItSystemUsageWriteService writeService,
            IItSystemUsageWriteModelMapper writeModelMapper)
        {
            _itSystemUsageService = itSystemUsageService;
            _responseMapper = responseMapper;
            _writeService = writeService;
            _writeModelMapper = writeModelMapper;
        }

        /// <summary>
        /// Returns all IT-System usages available to the current user
        /// </summary>
        /// <param name="organizationUuid">Query usages within a specific organization</param>
        /// <param name="relatedToSystemUuid">Query by systems with outgoing relations related to another system</param>
        /// <param name="relatedToSystemUsageUuid">Query by system usages with outgoing relations to a specific system usage (more narrow search than using system id)</param>
        /// <param name="relatedToContractUuid">Query by contracts with relations to a system usage</param>
        /// <param name="systemUuid">Query usages of a specific system</param>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<ItSystemUsageResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetItSystemUsages(
            [NonEmptyGuid] Guid? organizationUuid = null,
            [NonEmptyGuid] Guid? relatedToSystemUuid = null,
            [NonEmptyGuid] Guid? relatedToSystemUsageUuid = null,
            [NonEmptyGuid] Guid? relatedToContractUuid = null,
            [NonEmptyGuid] Guid? systemUuid = null,
            string systemNameContent = null,
            [FromUri] BoundedPaginationQuery paginationQuery = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var conditions = new List<IDomainQuery<ItSystemUsage>>();

            if (organizationUuid.HasValue)
                conditions.Add(new QueryByOrganizationUuid<ItSystemUsage>(organizationUuid.Value));

            if (relatedToSystemUuid.HasValue)
                conditions.Add(new QueryByRelationToSystem(relatedToSystemUuid.Value));

            if (relatedToSystemUsageUuid.HasValue)
                conditions.Add(new QueryByRelationToSystemUsage(relatedToSystemUsageUuid.Value));

            if (relatedToContractUuid.HasValue)
                conditions.Add(new QueryByRelationToContract(relatedToContractUuid.Value));

            if (systemUuid.HasValue)
                conditions.Add(new QueryBySystemUuid(systemUuid.Value));

            if (!string.IsNullOrWhiteSpace(systemNameContent))
                conditions.Add(new QueryBySystemNameContent(systemNameContent));

            return _itSystemUsageService
                .Query(conditions.ToArray())
                .OrderBy(itSystemUsage => itSystemUsage.Id)
                .Page(paginationQuery).AsEnumerable()
                .Select(_responseMapper.MapSystemUsageDTO).ToList()
                .Transform(Ok);
        }

        /// <summary>
        /// Returns a specific IT-System usage (a specific IT-System in a specific Organization)
        /// </summary>
        /// <param name="systemUsageUuid">UUID of the system usage entity</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{systemUsageUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult GetItSystemUsage([NonEmptyGuid] Guid systemUsageUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _itSystemUsageService
                .GetByUuid(systemUsageUuid)
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Deletes a system usage.
        /// NOTE: this action also clears any incoming relation e.g. relations from other system usages, contracts, projects or data processing registrations.
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{systemUsageUuid}")]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult DeleteItSystemUsage([NonEmptyGuid] Guid systemUsageUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Delete(systemUsageUuid)
                .Match(FromOperationError, () => StatusCode(HttpStatusCode.NoContent));
        }

        /// <summary>
        /// Creates an IT-System usage
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.Created, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Conflict, description: "Another system usage has already been created for the system within the specified organization")]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PostItSystemUsage([FromBody] CreateItSystemUsageRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Create(new SystemUsageCreationParameters(request.SystemUuid, request.OrganizationUuid, _writeModelMapper.FromPOST(request)))
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(MapSystemCreatedResponse, FromOperationError);
        }

        /// <summary>
        /// Updates the properties of the system usage.
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsage([NonEmptyGuid] Guid systemUsageUuid, [FromBody] UpdateItSystemUsageRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updateParameters = _writeModelMapper.FromPUT(request);

            return _writeService
                .Update(systemUsageUuid, updateParameters)
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the general properties of the system usage.
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/general")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageGeneralProperties([NonEmptyGuid] Guid systemUsageUuid, [FromBody] GeneralDataUpdateRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(systemUsageUuid, new SystemUsageUpdateParameters
                {
                    GeneralProperties = _writeModelMapper.MapGeneralDataUpdate(request)
                })
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the role assignments of the system usage.
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/roles")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageRoleAssignments([NonEmptyGuid] Guid systemUsageUuid, [FromBody] IEnumerable<RoleAssignmentRequestDTO> request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(systemUsageUuid, new SystemUsageUpdateParameters
                {
                    Roles = _writeModelMapper.MapRoles(request)
                })
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the kle deviations of the system usage
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/kle")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageKleDeviations([NonEmptyGuid] Guid systemUsageUuid, [FromBody] LocalKLEDeviationsRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(systemUsageUuid, new SystemUsageUpdateParameters
                {
                    KLE = _writeModelMapper.MapKle(request)
                })
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the external references of the system usage
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/external-references")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageExternalReferences([NonEmptyGuid] Guid systemUsageUuid, [FromBody][Required] IEnumerable<ExternalReferenceDataDTO> request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(systemUsageUuid, new SystemUsageUpdateParameters
                {
                    ExternalReferences = _writeModelMapper.MapReferences(request).FromNullable()
                })
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the archiving registrations of the system usage
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/archiving")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageArchiving([NonEmptyGuid] Guid systemUsageUuid, [FromBody] ArchivingWriteRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(systemUsageUuid, new SystemUsageUpdateParameters
                {
                    Archiving = _writeModelMapper.MapArchiving(request)
                })
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the GDPR registrations of the system usage
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/gdpr")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageGDPR([NonEmptyGuid] Guid systemUsageUuid, [FromBody][Required] GDPRWriteRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(systemUsageUuid, new SystemUsageUpdateParameters
                {
                    GDPR = _writeModelMapper.MapGDPR(request)
                })
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the organizational references for the system usage
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/organization-usage")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItSystemUsageResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageOrganizationUsage([NonEmptyGuid] Guid systemUsageUuid, [FromBody] OrganizationUsageWriteRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .Update(systemUsageUuid, new SystemUsageUpdateParameters
                {
                    OrganizationalUsage = _writeModelMapper.MapOrganizationalUsage(request)
                })
                .Select(_responseMapper.MapSystemUsageDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Creates a system relation
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("{systemUsageUuid}/system-relations")]
        [SwaggerResponse(HttpStatusCode.Created, Type = typeof(SystemRelationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PostSystemUsageRelation([NonEmptyGuid] Guid systemUsageUuid, [FromBody][Required] SystemRelationWriteRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var systemRelationParameters = _writeModelMapper.MapRelation(request);

            return _writeService
                .CreateSystemRelation(systemUsageUuid, systemRelationParameters)
                .Select(_responseMapper.MapSystemRelationDTO)
                .Match(relationDTO => Created($"{Request.RequestUri.AbsoluteUri.TrimEnd('/')}/{systemUsageUuid}/system-relations/{relationDTO.Uuid}", relationDTO), FromOperationError);
        }

        /// <summary>
        /// Gets a specific relation
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <param name="systemRelationUuid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{systemUsageUuid}/system-relations/{systemRelationUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(SystemRelationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult GetSystemUsageRelation([NonEmptyGuid] Guid systemUsageUuid, [NonEmptyGuid] Guid systemRelationUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _itSystemUsageService
                .GetByUuid(systemUsageUuid)
                .Bind(usage =>
                    usage.GetUsageRelation(systemRelationUuid)
                        .Match<Result<SystemRelation, OperationError>>
                        (
                        systemRelation => systemRelation,
                        () => new OperationError("Relation not found on system usage", OperationFailure.NotFound))
                    )
                .Select(_responseMapper.MapSystemRelationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Updates the system relation
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <param name="systemRelationUuid"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{systemUsageUuid}/system-relations/{systemRelationUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(SystemRelationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PutSystemUsageRelation([NonEmptyGuid] Guid systemUsageUuid, [NonEmptyGuid] Guid systemRelationUuid, [FromBody] SystemRelationWriteRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var systemRelationParameters = _writeModelMapper.MapRelation(request);

            return _writeService
                .UpdateSystemRelation(systemUsageUuid, systemRelationUuid, systemRelationParameters)
                .Select(_responseMapper.MapSystemRelationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Deletes a system relation
        /// </summary>
        /// <param name="systemUsageUuid"></param>
        /// <param name="systemRelationUuid"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{systemUsageUuid}/system-relations/{systemRelationUuid}")]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult DeleteSystemUsageRelation([NonEmptyGuid] Guid systemUsageUuid, [NonEmptyGuid] Guid systemRelationUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .DeleteSystemRelation(systemUsageUuid, systemRelationUuid)
                .Match(FromOperationError, () => StatusCode(HttpStatusCode.NoContent));
        }

        private CreatedNegotiatedContentResult<ItSystemUsageResponseDTO> MapSystemCreatedResponse(ItSystemUsageResponseDTO dto)
        {
            return Created($"{Request.RequestUri.AbsoluteUri.TrimEnd('/')}/{dto.Uuid}", dto);
        }
    }
}