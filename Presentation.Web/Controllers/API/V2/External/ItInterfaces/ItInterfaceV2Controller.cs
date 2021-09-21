﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;
using Core.Abstractions.Extensions;
using Core.ApplicationServices.Interface;
using Core.ApplicationServices.Model.Interface;
using Core.ApplicationServices.RightsHolders;
using Core.DomainModel.ItSystem;
using Core.DomainServices.Queries;
using Core.DomainServices.Queries.Interface;
using Presentation.Web.Controllers.API.V2.Mapping;
using Presentation.Web.Extensions;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Request.Interface;
using Presentation.Web.Models.API.V2.Response.Interface;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.External.ItInterfaces
{
    [RoutePrefix("api/v2")]
    public class ItInterfaceV2Controller : ExternalBaseController
    {
        private readonly IItInterfaceRightsHolderService _rightsHolderService;
        private readonly IItInterfaceService _itInterfaceService;

        public ItInterfaceV2Controller(IItInterfaceRightsHolderService rightsHolderService, IItInterfaceService itInterfaceService)
        {
            _rightsHolderService = rightsHolderService;
            _itInterfaceService = itInterfaceService;
        }


        /// <summary>
        /// Creates a new IT-Interface based on given input values
        /// </summary>
        /// <param name="request">A collection of specific IT-Interface values</param>
        /// <returns>Location header is set to uri for newly created IT-Interface</returns>
        [HttpPost]
        [AllowRightsHoldersAccess]
        [Route("rightsholder/it-interfaces")]
        [SwaggerResponseRemoveDefaults]
        [SwaggerResponse(HttpStatusCode.Created, Type = typeof(RightsHolderItInterfaceResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Conflict)]
        public IHttpActionResult PostItInterfaceAsRightsHolder([FromBody] RightsHolderCreateItInterfaceRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var creationParameters = new RightsHolderItInterfaceCreationParameters(
                request.Uuid,
                request.ExposedBySystemUuid,
                request.Name,
                request.InterfaceId,
                request.Version,
                request.Description,
                request.UrlReference);

            return _rightsHolderService
                .CreateNewItInterface(request.RightsHolderUuid, creationParameters)
                .Select(ToRightsHolderItInterfaceResponseDTO)
                .Match(MapItInterfaceCreatedResponse, FromOperationError);
        }

        /// <summary>
        /// Returns all IT-Interfaces for which the user has rights holders access
        /// </summary>
        /// <param name="rightsHolderUuid">Uuid of the organization you want interfaces from. If not provided all available interfaces (based on access rights) will be returned</param>
        /// <param name="includeDeactivated">If set to true, the response will also include deactivated it-interfaces</param>
        /// <param name="changedSinceGtEq">Include only changes which were modified at or following the provided value</param>
        /// <returns></returns>
        [HttpGet]
        [AllowRightsHoldersAccess]
        [Route("rightsholder/it-interfaces")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<RightsHolderItInterfaceResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult GetItInterfacesAsRightsHolder(
            [NonEmptyGuid] Guid? rightsHolderUuid = null,
            bool? includeDeactivated = false,
            DateTime? changedSinceGtEq = null,
            [FromUri] BoundedPaginationQuery pagination = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var refinements = new List<IDomainQuery<ItInterface>>();

            if (includeDeactivated != true)
                refinements.Add(new QueryByEnabledEntitiesOnly<ItInterface>());

            if (changedSinceGtEq.HasValue)
                refinements.Add(new QueryByChangedSinceGtEq<ItInterface>(changedSinceGtEq.Value));

            return _rightsHolderService
                .GetInterfacesWhereAuthenticatedUserHasRightsHolderAccess(refinements, rightsHolderUuid)
                .Match(
                    success => success
                        .OrderByDefaultConventions(changedSinceGtEq.HasValue)
                        .Page(pagination)
                        .ToList()
                        .Select(ToRightsHolderItInterfaceResponseDTO)
                        .Transform(Ok),
                    FromOperationError);
        }

        /// <summary>
        /// Returns requested IT-Interface
        /// </summary>
        /// <param name="uuid">Specific IT-Interface UUID</param>
        /// <returns>Specific data related to the IT-Interface</returns>
        [HttpGet]
        [AllowRightsHoldersAccess]
        [Route("rightsholder/it-interfaces/{uuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(RightsHolderItInterfaceResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetItInterfaceAsRightsHolder([NonEmptyGuid] Guid uuid)
        {
            return _rightsHolderService
                .GetInterfaceAsRightsHolder(uuid)
                .Select(ToRightsHolderItInterfaceResponseDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Sets IT-Interface values
        /// </summary>
        /// <param name="uuid">Specific IT-Interface UUID</param>
        /// <returns>The updated IT-Interface</returns>
        [HttpPut]
        [AllowRightsHoldersAccess]
        [Route("rightsholder/it-interfaces/{uuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(RightsHolderItInterfaceResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult PutItInterfaceAsRightsHolder([NonEmptyGuid] Guid uuid, [FromBody] RightsHolderWritableItInterfacePropertiesDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updateParameters = new RightsHolderItInterfaceUpdateParameters(
                request.ExposedBySystemUuid,
                request.Name,
                request.InterfaceId,
                request.Version,
                request.Description,
                request.UrlReference);

            return _rightsHolderService
                .UpdateItInterface(uuid, updateParameters)
                .Select(ToRightsHolderItInterfaceResponseDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Deactivates an IT-Interface
        /// </summary>
        /// <param name="uuid">Specific IT-Interface UUID</param>
        /// <param name="deactivationReasonDTO">Reason for deactivation</param>
        /// <returns>No content</returns>
        [HttpDelete]
        [AllowRightsHoldersAccess]
        [Route("rightsholder/it-interfaces/{uuid}")]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult DeactivateItInterfaceAsRightsHolder([NonEmptyGuid] Guid uuid, [FromBody] DeactivationReasonRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _rightsHolderService
                .Deactivate(uuid, request.DeactivationReason)
                .Select(ToRightsHolderItInterfaceResponseDTO)
                .Match(_ => StatusCode(HttpStatusCode.NoContent), FromOperationError);
        }

        /// <summary>
        /// Returns IT-Interfaces available to the user
        /// </summary>
        /// <param name="exposedBySystemUuid">IT-System UUID filter</param>
        /// <param name="includeDeactivated">If set to true, the response will also include deactivated it-interfaces</param>
        /// <param name="changedSinceGtEq">Include only changes which were modified at or following the provided value</param>
        /// <returns></returns>
        [HttpGet]
        [Route("it-interfaces")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<ItInterfaceResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult GetItInterfaces(
            [NonEmptyGuid] Guid? exposedBySystemUuid = null,
            bool? includeDeactivated = false,
            DateTime? changedSinceGtEq = null,
            [FromUri] BoundedPaginationQuery pagination = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var refinements = new List<IDomainQuery<ItInterface>>();

            if (exposedBySystemUuid.HasValue)
                refinements.Add(new QueryByExposingSystem(exposedBySystemUuid.Value));

            if (includeDeactivated != true)
                refinements.Add(new QueryByEnabledEntitiesOnly<ItInterface>());

            if (changedSinceGtEq.HasValue)
                refinements.Add(new QueryByChangedSinceGtEq<ItInterface>(changedSinceGtEq.Value));

            return _itInterfaceService
                .GetAvailableInterfaces(refinements.ToArray())
                .OrderByDefaultConventions(changedSinceGtEq.HasValue)
                .Page(pagination)
                .ToList()
                .Select(ToStakeHolderItInterfaceResponseDTO)
                .Transform(Ok);
        }

        /// <summary>
        /// Returns requested IT-Interface
        /// </summary>
        /// <param name="uuid">Specific IT-Interface UUID</param>
        /// <returns>Specific data related to the IT-Interface</returns>
        [HttpGet]
        [Route("it-interfaces/{uuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ItInterfaceResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetItInterface([NonEmptyGuid] Guid uuid)
        {
            return _itInterfaceService
                .GetInterface(uuid)
                .Select(ToStakeHolderItInterfaceResponseDTO)
                .Match(Ok, FromOperationError);
        }

        private static RightsHolderItInterfaceResponseDTO ToRightsHolderItInterfaceResponseDTO(ItInterface itInterface)
        {
            var dto = new RightsHolderItInterfaceResponseDTO();
            MapBaseInformation(itInterface, dto);
            return dto;
        }

        private static ItInterfaceResponseDTO ToStakeHolderItInterfaceResponseDTO(ItInterface itInterface)
        {
            var dto = new ItInterfaceResponseDTO
            {
                LastModified = itInterface.LastChanged,
                LastModifiedBy = itInterface.LastChangedByUser.Transform(user => user.MapIdentityNamePairDTO())
            };
            MapBaseInformation(itInterface, dto);
            return dto;
        }

        private static void MapBaseInformation<T>(ItInterface input, T outputDTO) where T : BaseItInterfaceResponseDTO
        {
            outputDTO.Uuid = input.Uuid;
            outputDTO.ExposedBySystem = input.ExhibitedBy?.ItSystem?.Transform(exposingSystem => exposingSystem.MapIdentityNamePairDTO());
            outputDTO.Name = input.Name;
            outputDTO.InterfaceId = input.ItInterfaceId;
            outputDTO.Version = input.Version;
            outputDTO.Description = input.Description;
            outputDTO.UrlReference = input.Url;
            outputDTO.Deactivated = input.Disabled;
            outputDTO.Created = input.Created;
            outputDTO.CreatedBy = input.ObjectOwner.MapIdentityNamePairDTO();
        }

        private CreatedNegotiatedContentResult<RightsHolderItInterfaceResponseDTO> MapItInterfaceCreatedResponse(RightsHolderItInterfaceResponseDTO dto)
        {
            return Created($"{Request.RequestUri.AbsoluteUri.TrimEnd('/')}/{dto.Uuid}", dto);
        }
    }
}