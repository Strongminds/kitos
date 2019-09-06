﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.ItSystemUsageMigration;
using Core.ApplicationServices.Model.Result;
using Core.DomainModel.ItSystem;
using Core.DomainServices.Authorization;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API
{
    [PublicApi]
    [RoutePrefix("api/v1/ItSystemUsageMigration")]
    public class ItSystemUsageMigrationController : BaseApiController
    {
        private IItSystemUsageMigrationService _itSystemUsageMigrationService;

        public ItSystemUsageMigrationController(IItSystemUsageMigrationService itSystemUsageMigrationService, IAuthorizationContext authContext) 
            : base(authContext)
        {
            _itSystemUsageMigrationService = itSystemUsageMigrationService;
        }

        [HttpGet]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK)]
        public HttpResponseMessage GetMigrationConflicts([FromUri]int usageId, [FromUri]int toSystemId)
        {
            var result = _itSystemUsageMigrationService.GetMigrationConflicts(usageId, toSystemId);
            switch (result.Status)
            {
                case OperationResult.Ok:
                    return Ok(result.ResultValue);
                default:
                    return CreateResponse(HttpStatusCode.InternalServerError,
                        "An error occured when trying to get Unused It Systems");
            }
            
        }

        [HttpPost]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK)]
        public HttpResponseMessage ExecuteMigration([FromUri]int usageId, [FromUri]int toSystemId)
        {
            _itSystemUsageMigrationService.toExecute(usageId, toSystemId);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("UnusedItSystems")]
        [SwaggerResponse(HttpStatusCode.OK, Type=typeof(IEnumerable<ItSystemSimpleDTO>))]
        public HttpResponseMessage GetUnusedItSystemsBySearchAndOrganization(
            [FromUri]int organizationId, 
            [FromUri]string nameContent, 
            [FromUri]int numberOfItSystems, 
            [FromUri]bool getPublicFromOtherOrganizations)
        {
            try
            {
                if (GetOrganizationReadAccessLevel(organizationId) < OrganizationDataReadAccessLevel.Public)
                {
                    return Forbidden();
                }
                if (string.IsNullOrWhiteSpace(nameContent))
                {
                    return BadRequest();
                }
                if (numberOfItSystems < 1)
                {
                    return BadRequest();
                }

                var result = _itSystemUsageMigrationService.GetUnusedItSystemsByOrganization(organizationId, nameContent, numberOfItSystems, getPublicFromOtherOrganizations);

                switch (result.Status)
                {
                    case OperationResult.Ok:
                        return Ok(MapItSystemToItSystemUsageMigrationDto(result.ResultValue));
                    case OperationResult.Forbidden:
                        return Forbidden();
                    case OperationResult.NotFound:
                        return NotFound();
                    default:
                        return CreateResponse(HttpStatusCode.InternalServerError,
                            "An error occured when trying to get Unused It Systems");
                }

            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        private IEnumerable<ItSystemSimpleDTO> MapItSystemToItSystemUsageMigrationDto(IReadOnlyList<ItSystem> input)
        {
            return input.Select(itSystem => new ItSystemSimpleDTO() {Id = itSystem.Id, Name = itSystem.Name}).ToList();
        }
        
    }
}