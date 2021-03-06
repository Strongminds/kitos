﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using System.Net;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices;
using Core.DomainModel.Organization;
using Core.DomainModel.ItSystem;
using Core.DomainServices.Authorization;
using Core.DomainServices.Extensions;
using Core.DomainServices.Repositories.Organization;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.OData;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.OData
{
    [PublicApi]
    public class ItSystemUsagesController : BaseEntityController<ItSystemUsage>
    {
        private readonly IGenericRepository<OrganizationUnit> _orgUnitRepository;
        private readonly IGenericRepository<AccessType> _accessTypeRepository;
        private readonly IOrganizationUnitRepository _organizationUnitRepository;

        public ItSystemUsagesController(
            IGenericRepository<ItSystemUsage> repository,
            IGenericRepository<OrganizationUnit> orgUnitRepository,
            IGenericRepository<AccessType> accessTypeRepository,
            IOrganizationUnitRepository organizationUnitRepository)
            : base(repository)
        {
            _orgUnitRepository = orgUnitRepository;
            _accessTypeRepository = accessTypeRepository;
            _organizationUnitRepository = organizationUnitRepository;
        }

        /// <summary>
        /// Henter alle organisationens IT-Systemanvendelser.
        /// </summary>
        /// <param name="orgKey"></param>
        /// <returns></returns>
        [EnableQuery(MaxExpansionDepth = 4)] // MaxExpansionDepth is 4 because we need to do MainContract($expand=ItContract($expand=Supplier))
        [ODataRoute("Organizations({orgKey})/ItSystemUsages")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IEnumerable<ItSystemUsage>>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [RequireTopOnOdataThroughKitosToken]
        public IHttpActionResult GetItSystems(int orgKey)
        {
            //Usages are local so full access is required
            var accessLevel = GetOrganizationReadAccessLevel(orgKey);
            if (accessLevel < OrganizationDataReadAccessLevel.All)
            {
                return Forbidden();
            }

            var result = Repository
                .AsQueryable()
                .ByOrganizationId(orgKey);

            return Ok(result);
        }

        /// <summary>
        /// Henter alle IT-Systemanvendelser for den pågældende organisationsenhed
        /// </summary>
        /// <param name="orgKey"></param>
        /// <param name="unitKey"></param>
        /// <returns></returns>
        [EnableQuery(MaxExpansionDepth = 4)] // MaxExpansionDepth is 4 because we need to do MainContract($expand=ItContract($expand=Supplier))
        [ODataRoute("Organizations({orgKey})/OrganizationUnits({unitKey})/ItSystemUsages")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IEnumerable<ItSystemUsage>>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [RequireTopOnOdataThroughKitosToken]
        public IHttpActionResult GetItSystemsByOrgUnit(int orgKey, int unitKey)
        {
            //Usages are local so full access is required
            if (GetOrganizationReadAccessLevel(orgKey) != OrganizationDataReadAccessLevel.All)
            {
                return Forbidden();
            }

            var orgUnitTreeIds = _organizationUnitRepository.GetIdsOfSubTree(orgKey, unitKey).ToList();

            var result = Repository
                .AsQueryable()
                .ByOrganizationId(orgKey)
                .Where(usage => usage.ResponsibleUsage != null && orgUnitTreeIds.Contains(usage.ResponsibleUsage.OrganizationUnitId));

            return Ok(result);
        }

        [AcceptVerbs("POST", "PUT")]
        [ODataRoute("ItSystemUsages({key})/AccessTypes/{accessTypeId}")]
        public IHttpActionResult CreateRef(int key, int accessTypeId)
        {
            var itSystemUsage = Repository.GetByKey(key);
            if (itSystemUsage == null)
            {
                return NotFound();
            }

            if (!AllowModify(itSystemUsage))
            {
                return Forbidden();
            }

            var accessType = _accessTypeRepository.GetByKey(accessTypeId);
            if (accessType == null)
            {
                return BadRequest("Invalid accessTypeId");
            }

            itSystemUsage.AccessTypes.Add(accessType);

            Repository.Save();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [AcceptVerbs("DELETE")]
        [ODataRoute("ItSystemUsages({key})/AccessTypes/{accessTypeId}")]
        public IHttpActionResult DeleteRef(int key, int accessTypeId)
        {
            var itSystemUsage = Repository.GetByKey(key);
            if (itSystemUsage == null)
            {
                return NotFound();
            }

            if (!AllowModify(itSystemUsage))
            {
                return Forbidden();
            }

            var accessType = _accessTypeRepository.GetByKey(accessTypeId);

            if (accessType == null)
            {
                return BadRequest("Invalid accessTypeId");
            }

            itSystemUsage.AccessTypes.Remove(accessType);

            Repository.Save();

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
