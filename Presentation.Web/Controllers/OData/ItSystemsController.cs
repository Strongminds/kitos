﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Core.DomainModel.Events;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Core.DomainModel.ItSystem;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Core.DomainServices.Extensions;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.OData;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.OData
{
    [PublicApi]
    public class ItSystemsController : BaseEntityController<ItSystem>
    {
        public ItSystemsController(IGenericRepository<ItSystem> repository)
            : base(repository)
        {
        }

        /// <summary>
        /// Henter alle organisationens IT-Systemer samt offentlige IT-systemer fra andre organisationer.
        /// Resultatet filtreres i hht. brugerens læserettigheder i den opgældende organisation, samt på tværs af organisationer.
        /// </summary>
        /// <param name="orgKey"></param>
        /// <returns></returns>
        [EnableQuery]
        [ODataRoute("Organizations({orgKey})/ItSystems")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IEnumerable<ItSystem>>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [RequireTopOnOdataThroughKitosToken]
        public IHttpActionResult GetItSystems(int orgKey)
        {
            var readAccessLevel = GetOrganizationReadAccessLevel(orgKey);
            if (readAccessLevel == OrganizationDataReadAccessLevel.None)
            {
                return Forbidden();
            }

            var result = Repository
                    .AsQueryable()
                    .ByOrganizationDataAndPublicDataFromOtherOrganizations(orgKey, readAccessLevel, GetCrossOrganizationReadAccessLevel());

            return Ok(result);
        }

        public override IHttpActionResult Patch(int key, Delta<ItSystem> delta)
        {
            var itSystem = Repository.GetByKey(key);
            
            if (itSystem == null)
                return NotFound();

            if (AttemptToChangeUuid(delta))
                return BadRequest("Cannot change Uuid");

            var disabledBefore = itSystem.Disabled;
            var result = base.Patch(key, delta);
            if (disabledBefore != itSystem.Disabled)
            {
                DomainEvents.Raise(new EnabledStatusChanged<ItSystem>(itSystem, disabledBefore, itSystem.Disabled));
            }

            return result;
        }

        private bool AttemptToChangeUuid(Delta<ItSystem> delta)
        {
            return delta.TryGetPropertyValue(nameof(Core.DomainModel.User.Uuid), out _);
        }

        [ODataRoute("ItSystems")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IEnumerable<ItSystem>>))]
        [RequireTopOnOdataThroughKitosToken]
        public override IHttpActionResult Get()
        {
            return base.Get();
        }

        [NonAction]
        public override IHttpActionResult Delete(int key) => throw new NotSupportedException();

        [NonAction]
        public override IHttpActionResult Post(int organizationId, ItSystem entity) => throw new NotSupportedException();
    }
}
