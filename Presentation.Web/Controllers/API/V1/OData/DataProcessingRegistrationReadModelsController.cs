using System;
using System.Net;
using Core.Abstractions.Types;
using Core.ApplicationServices.GDPR;
using Core.DomainModel.GDPR.Read;
using Core.DomainModel.Organization;
using Core.DomainServices.Generic;
using Presentation.Web.Infrastructure.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Presentation.Web.Controllers.API.V1.OData
{
    /// <summary>
    /// Search API used for DataProcessingRegistrations
    /// </summary>
    [InternalApi]
    public class DataProcessingRegistrationReadModelsController : BaseOdataController
    {
        private readonly IDataProcessingRegistrationReadModelService _dataProcessingRegistrationReadModelService;
        private readonly IEntityIdentityResolver _identityResolver;

        public DataProcessingRegistrationReadModelsController(IDataProcessingRegistrationReadModelService dataProcessingRegistrationReadModelService, 
             IEntityIdentityResolver identityResolver)
        {
            _dataProcessingRegistrationReadModelService = dataProcessingRegistrationReadModelService;
            _identityResolver = identityResolver;
        }

        [EnableQuery]
        [Route("Organizations({organizationId})/DataProcessingRegistrationReadModels")]
        public IActionResult Get([FromRoute]int organizationId)
        {
            return GetOverviewReadModels(organizationId);
        }

        /// <summary>
        /// V2 style OData endpoint suited for consumption by clients using UUID's for entity identity
        /// </summary>
        /// <param name="organizationUuid"></param>
        /// <returns></returns>
        [EnableQuery(MaxNodeCount = 300)]
        [Route("DataProcessingRegistrationReadModels")]
        public IActionResult GetByUuid(Guid organizationUuid, Guid? responsibleOrganizationUnitUuid = null)
        {
            var orgDbId = _identityResolver.ResolveDbId<Organization>(organizationUuid);
            if (orgDbId.IsNone)
            {
                return FromOperationError(new OperationError("Invalid org id", OperationFailure.NotFound));
            }

            return GetOverviewReadModels(orgDbId.Value);
        }

        private IActionResult GetOverviewReadModels(int organizationId)
        {
            return _dataProcessingRegistrationReadModelService
                .GetByOrganizationId(organizationId)
                .Match(onSuccess: Ok, onFailure: FromOperationError);
        }
    }
}




