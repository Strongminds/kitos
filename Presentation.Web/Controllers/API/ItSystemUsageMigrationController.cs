﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.ItSystemUsageMigration;
using Core.ApplicationServices.Model.ItSystemUsage;
using Core.ApplicationServices.Model.Result;
using Core.DomainModel;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
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
            //TODO authorization
            var res = _itSystemUsageMigrationService.GetSystemUsageMigration(usageId, toSystemId);
            switch (res.Status)
            {
                case OperationResult.Ok :
                    return Ok(MapItSystemUsageMigration(res.ResultValue));
                default:
                    return CreateResponse(HttpStatusCode.InternalServerError,
                        "An error occured when trying to get migration consequences");
            }

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

        private static ItSystemUsageMigrationDTO MapItSystemUsageMigration(
            ItSystemUsageMigration input)
        {
            return new ItSystemUsageMigrationDTO()
            {
                TargetUsage = new NamedEntityDTO(){Id = input.ItSystemUsage.Id,Name = input.ItSystemUsage.LocalCallName ?? input.FromItSystem.Name} ,
                FromSystem = MapToNamedEntityDTO(input.FromItSystem),
                ToSystem = MapToNamedEntityDTO(input.ToItSystem),
                AffectedItProjects = input.AffectedProjects.Select(MapToNamedEntityDTO).ToList(),
                AffectedContracts = input.AffectedContracts.Select(MapToItContractItSystemUsageDTO).ToList()
            };
        }

        private static NamedEntityDTO MapToNamedEntityDTO<T>(T input) where T : IEntity, IHasName
        {
            return new NamedEntityDTO(){Id = input.Id, Name = input.Name};
        }

        private static ItSystemUsageContractMigrationDTO MapToItContractItSystemUsageDTO(ItSystemUsageContractMigration input)
        {
            return new ItSystemUsageContractMigrationDTO()
            {
                Contract = MapToNamedEntityDTO(input.Contract),
                AffectedInterfaceUsages = input.AffectedInterfaceUsages.Select(MapToInterfaceUsageDTO).ToList(),
                InterfaceExhibitUsagesToBeDeleted = input.ExhibitUsagesToBeDeleted.Select(MapToInterfaceExhibitUsageDTO).ToList()
            };
        }

        private static NamedEntityDTO MapToInterfaceExhibitUsageDTO(ItInterfaceExhibitUsage interfaceExhibit)
        {
            return new NamedEntityDTO()
            {
                Id = interfaceExhibit.ItInterfaceExhibitId,
                Name = interfaceExhibit.ItInterfaceExhibit.ItInterface.Name
            };
        }

        private static NamedEntityDTO MapToInterfaceUsageDTO(ItInterfaceUsage interfaceUsage)
        {
            return new NamedEntityDTO()
            {
                Id = interfaceUsage.ItInterfaceId,
                Name = interfaceUsage.ItInterface.Name
            };
        }
    }
}