using Core.ApplicationServices.GDPR;
using Core.DomainServices.Queries;
using Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Types.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.ApplicationServices.GDPR.Write;
using Core.DomainModel.GDPR;
using Core.DomainServices.Generic;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Extensions;
using Presentation.Web.Models.API.V2.Response.DataProcessing;
using Presentation.Web.Models.API.V2.Internal.Response.Roles;
using Presentation.Web.Models.API.V2.Request.Generic.Roles;
using Presentation.Web.Models.API.V2.Response.Generic.Identity;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Models.API.V2.Response.Organization;

namespace Presentation.Web.Controllers.API.V2.Internal.DataProcessingRegistrations
{
    /// <summary>
    /// Internal API for the data processing registrations stored in KITOS.
    /// </summary>
    [Route("api/v2/internal/data-processing-registrations")]
    public class DataProcessingRegistrationInternalV2Controller : InternalApiV2Controller
    {
        private readonly IDataProcessingRegistrationApplicationService _dataProcessingRegistrationService;
        private readonly IDataProcessingRegistrationResponseMapper _responseMapper;
        private readonly IDataProcessingRegistrationWriteService _writeService;
        private readonly IEntityIdentityResolver _identityResolver;

        public DataProcessingRegistrationInternalV2Controller(IDataProcessingRegistrationApplicationService dataProcessingRegistrationService, 
            IDataProcessingRegistrationResponseMapper responseMapper, 
            IDataProcessingRegistrationWriteService writeService, 
            IEntityIdentityResolver identityResolver)
        {
            _dataProcessingRegistrationService = dataProcessingRegistrationService;
            _responseMapper = responseMapper;
            _writeService = writeService;
            _identityResolver = identityResolver;
        }

        /// <summary>
        /// Shallow search endpoint returning all Data Processing Registrations available to the current user
        /// </summary>
        /// <param name="nameContains">Include only dprs with a name that contains the content in the parameter</param>
        /// <param name="orderByProperty">Ordering property</param>
        /// <returns></returns>
        [HttpGet]
        [Route("search")]
        public IActionResult GetItSystems(
            [NonEmptyGuid] Guid organizationUuid,
            string nameContains = null,
            CommonOrderByProperty? orderByProperty = null,
            [FromQuery] BoundedPaginationQuery paginationQuery = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var conditions = new List<IDomainQuery<DataProcessingRegistration>> { new QueryByOrganizationUuid<DataProcessingRegistration>(organizationUuid) };

            if(nameContains != null)
                conditions.Add(new QueryByPartOfName<DataProcessingRegistration>(nameContains));

            return _dataProcessingRegistrationService
                .Query(conditions.ToArray())
                .OrderApiResultsByDefaultConventions(false, orderByProperty)
                .Page(paginationQuery)
                .ToList()
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .ToList()
                .Transform(Ok);
        }

        /// <summary>
        /// Get roles assigned to the data processing registration
        /// </summary>
        /// <param name="dprUuid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{dprUuid}/roles")]
        public IActionResult GetAddRoleAssignments([NonEmptyGuid] Guid dprUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            return _dataProcessingRegistrationService
                .GetByUuid(dprUuid)
                .Select(x => x.Rights.ToList())
                .Select(rights => rights.Select(right => right.MapExtendedRoleAssignmentResponse()))
                .Match(Ok, FromOperationError);
        }

        /// Add role assignment to the data processing registration
        /// Constraint: Duplicates are not allowed (existing assignment of the same user/role)
        /// </summary>
        /// <param name="dprUuid"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{dprUuid}/roles/add")]
        public IActionResult PatchAddRoleAssignment([NonEmptyGuid] Guid dprUuid, [FromBody] RoleAssignmentRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .AddRole(dprUuid, request.ToUserRolePair())
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Remove an existing role assignment to the data processing registration
        /// </summary>
        /// <param name="dprUuid"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{dprUuid}/roles/remove")]
        public IActionResult PatchRemoveRoleAssignment([NonEmptyGuid] Guid dprUuid, [FromBody] RoleAssignmentRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return _writeService
                .RemoveRole(dprUuid, request.ToUserRolePair())
                .Select(_responseMapper.MapDataProcessingRegistrationDTO)
                .Match(Ok, FromOperationError);
        }

        [HttpGet]
        [Route("{dprUuid}/data-processors/available")]
        public IActionResult GetAvailableDataProcessors([NonEmptyGuid] Guid dprUuid,
            [FromQuery] string nameQuery = null, [FromQuery] int pageSize = 25)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var idResult = _identityResolver.ResolveDbId<DataProcessingRegistration>(dprUuid);
            if (idResult.IsNone)
                return FromOperationError(new OperationError($"DataProcessingRegistration with uuid: {dprUuid} was not found", OperationFailure.NotFound));

            return _dataProcessingRegistrationService
                .GetDataProcessorsWhichCanBeAssigned(idResult.Value, nameQuery, pageSize)
                .Match(organizations => Ok(organizations.Select(x => x.MapShallowOrganizationResponseDTO()).ToList()), FromOperationError);
        }

        [HttpGet]
        [Route("{dprUuid}/sub-data-processors/available")]
        public IActionResult GetAvailableSubDataProcessors([NonEmptyGuid] Guid dprUuid,
            [FromQuery] string nameQuery = null, [FromQuery] int pageSize = 25)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var idResult = _identityResolver.ResolveDbId<DataProcessingRegistration>(dprUuid);
            if (idResult.IsNone)
                return FromOperationError(new OperationError($"DataProcessingRegistration with uuid: {dprUuid} was not found", OperationFailure.NotFound));

            return _dataProcessingRegistrationService
                .GetSubDataProcessorsWhichCanBeAssigned(idResult.Value, nameQuery, pageSize)
                .Match(organizations => Ok(organizations.Select(x => x.MapShallowOrganizationResponseDTO()).ToList()), FromOperationError);
        }

        [HttpGet]
        [Route("{dprUuid}/system-usages/available")]
        public IActionResult GetAvailableSystemUsages([NonEmptyGuid] Guid dprUuid,
            [FromQuery] string nameQuery = null, [FromQuery] int pageSize = 25)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var idResult = _identityResolver.ResolveDbId<DataProcessingRegistration>(dprUuid);
            if (idResult.IsNone)
                return FromOperationError(new OperationError(
                    $"DataProcessingRegistration with uuid: {dprUuid} was not found", OperationFailure.NotFound));

            return _dataProcessingRegistrationService.GetSystemsWhichCanBeAssigned(idResult.Value, nameQuery, pageSize)
                .Match(systemUsages => Ok(systemUsages.Select(x => x.MapIdentityNamePairDTO()).ToList()), FromOperationError);
        }
    }
}

