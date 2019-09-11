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
using Presentation.Web.Models.ItSystemUsageMigration;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API
{
    [PublicApi]
    [RoutePrefix("api/v1/ItSystemUsageMigration")]
    public class ItSystemUsageMigrationController : BaseApiController
    {
        private readonly IItSystemUsageMigrationService _itSystemUsageMigrationService;

        public ItSystemUsageMigrationController(IItSystemUsageMigrationService itSystemUsageMigrationService, IAuthorizationContext authContext)
            : base(authContext)
        {
            _itSystemUsageMigrationService = itSystemUsageMigrationService;
        }

        [HttpGet]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK)]
        public HttpResponseMessage GetMigration([FromUri]int usageId, [FromUri]int toSystemId)
        {
            //TODO
            var affectedItProjects = new List<NamedEntityDTO>();
            affectedItProjects.Add(new NamedEntityDTO()
            {
                Id = 1,
                Name = "ItProject"
            });

            var affectedInterfaceUsages = new List<NamedEntityDTO>();
            affectedInterfaceUsages.Add(new NamedEntityDTO()
            {
                Id = 2,
                Name = "InterfaceUsage"
            });
            affectedInterfaceUsages.Add(new NamedEntityDTO()
            {
                Id = 22,
                Name = "InterfaceUsage22"
            });
            var affectedInterfaceExhibitUsages = new List<NamedEntityDTO>();
            affectedInterfaceExhibitUsages.Add(new NamedEntityDTO()
            {
                Id = 3,
                Name = "InterfaceExhibitUsage"
            });
            var affectedItContracts = new List<ItContractItSystemUsageDTO>();
            affectedItContracts.Add(new ItContractItSystemUsageDTO()
            {
                Contract = new NamedEntityDTO()
                {
                    Id = 4,
                    Name = "Contract"
                },
                AffectedInterfaceUsages = affectedInterfaceUsages,
                InterfaceExhibitUsagesToBeDeleted = affectedInterfaceExhibitUsages
            });

            return Ok(new ItSystemUsageMigrationDTO
            {
                TargetUsage = new NamedEntityDTO()
                {
                    Id = usageId,
                    Name = "ItSystemUsage"
                },
                FromSystem = new NamedEntityDTO()
                {
                    Id = 1,
                    Name = "FromSystem"
                },
                ToSystem = new NamedEntityDTO()
                {
                    Id = 2,
                    Name = "ToSystem"
                },
                AffectedItProjects = affectedItProjects,
                AffectedContracts = affectedItContracts
            });

        }

        [HttpPost]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK)]
        public HttpResponseMessage ExecuteMigration([FromUri]int usageId, [FromUri]int toSystemId)
        {
            //TODO
            return Ok();
        }

        [HttpGet]
        [Route("Accessibility")]
        public HttpResponseMessage GetAccessibilityLevel()
        {
            return Ok(new
            {
                CanExecuteMigration = _itSystemUsageMigrationService.CanExecuteMigration()
            });
        }

        [HttpGet]
        [Route("UnusedItSystems")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<ItSystemSimpleDTO>))]
        public HttpResponseMessage GetUnusedItSystemsBySearchAndOrganization(
            [FromUri]int organizationId,
            [FromUri]string nameContent,
            [FromUri]int numberOfItSystems,
            [FromUri]bool getPublicFromOtherOrganizations)
        {
            if (GetOrganizationReadAccessLevel(organizationId) < OrganizationDataReadAccessLevel.Public)
            {
                return Forbidden();
            }
            if (string.IsNullOrWhiteSpace(nameContent))
            {
                return Ok(MapItSystemToItSystemUsageMigrationDto(new List<ItSystem>()));
            }
            if (numberOfItSystems < 1 || numberOfItSystems > 25)
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

        private static IEnumerable<ItSystemSimpleDTO> MapItSystemToItSystemUsageMigrationDto(IReadOnlyList<ItSystem> input)
        {
            return input.Select(itSystem => new ItSystemSimpleDTO { Id = itSystem.Id, Name = itSystem.Name }).ToList();
        }

    }
}