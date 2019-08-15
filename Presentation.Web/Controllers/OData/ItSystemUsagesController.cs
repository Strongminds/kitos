﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices;
using System.Net;
using Core.DomainModel.Organization;
using Core.ApplicationServices;
using Core.DomainModel.ItSystem;
using Presentation.Web.Access;

namespace Presentation.Web.Controllers.OData
{
    public class ItSystemUsagesController : BaseEntityController<ItSystemUsage>
    {
        private readonly IGenericRepository<OrganizationUnit> _orgUnitRepository;
        private readonly IGenericRepository<AccessType> _accessTypeRepository;
        private readonly IOrganizationContextFactory _contextFactory;
        private readonly IAuthenticationService _authService;

        public ItSystemUsagesController(IGenericRepository<ItSystemUsage> repository, IGenericRepository<OrganizationUnit> orgUnitRepository, 
            IAuthenticationService authService, IGenericRepository<AccessType> accessTypeRepository, IOrganizationContextFactory contextFactory)
            : base(repository, authService)
        {
            _orgUnitRepository = orgUnitRepository;
            _accessTypeRepository = accessTypeRepository;
            _contextFactory = contextFactory;
            _authService = authService;
        }

        // GET /Organizations(1)/ItSystemUsages
        [EnableQuery(MaxExpansionDepth = 4)] // MaxExpansionDepth is 4 because we need to do MainContract($expand=ItContract($expand=Supplier))
        [ODataRoute("Organizations({orgKey})/ItSystemUsages")]
        public IHttpActionResult GetItSystems(int orgKey)
        {
            var organizationContext = _contextFactory.CreateOrganizationContext(orgKey);
            if (!organizationContext.AllowReads(UserId))
            {
                return Forbidden();
            }

            var result = Repository.AsQueryable().Where(m => m.OrganizationId == orgKey);

            return Ok(result);
        }

        [EnableQuery(MaxExpansionDepth = 4)] // MaxExpansionDepth is 4 because we need to do MainContract($expand=ItContract($expand=Supplier))
        [ODataRoute("Organizations({orgKey})/OrganizationUnits({unitKey})/ItSystemUsages")]
        public IHttpActionResult GetItSystemsByOrgUnit(int orgKey, int unitKey)
        {
            var organizationContext = _contextFactory.CreateOrganizationContext(orgKey);
            if (!organizationContext.AllowReads(UserId))
            {
                return Forbidden();
            }

            var systemUsages = new List<ItSystemUsage>();
            var queue = new Queue<int>();
            queue.Enqueue(unitKey);
            while (queue.Count > 0)
            {
                var orgUnitKey = queue.Dequeue();
                var orgUnit = _orgUnitRepository.AsQueryable()
                    .Include(x => x.Children)
                    .Include(x => x.Using.Select(y => y.ResponsibleItSystemUsage))
                    .First(x => x.OrganizationId == orgKey && x.Id == orgUnitKey);
                var responsible =
                    orgUnit.Using.Select(x => x.ResponsibleItSystemUsage).Where(x => x != null).ToList();
                systemUsages.AddRange(responsible);
                var childIds = orgUnit.Children.Select(x => x.Id);
                foreach (var childId in childIds)
                {
                    queue.Enqueue(childId);
                }
            }

            return Ok(systemUsages);
        }

        [AcceptVerbs("POST", "PUT")]
        public IHttpActionResult CreateRef([FromODataUri] int systemUsageKey, string navigationProperty, [FromBody] Uri link)
        {
            var itSystemUsage = Repository.GetByKey(systemUsageKey);
            if (itSystemUsage == null)
            {
                return NotFound();
            }

            var organizationContext = _contextFactory.CreateOrganizationContext(itSystemUsage.OrganizationId);
            if (!organizationContext.AllowUpdates(UserId, itSystemUsage))
            {
                return Forbidden();
            }

            switch (navigationProperty)
            {
                case "AccessTypes":
                    var relatedKey = GetKeyFromUri<int>(Request, link);
                    var accessType = _accessTypeRepository.GetByKey(relatedKey);
                    if (accessType == null)
                    {
                        return NotFound();
                    }

                    itSystemUsage.AccessTypes.Add(accessType);
                    break;

                default:
                    return StatusCode(HttpStatusCode.NotImplemented);
            }

            Repository.Save();
            return StatusCode(HttpStatusCode.NoContent);
        }

        public IHttpActionResult DeleteRef([FromODataUri] int key, [FromODataUri] string relatedKey, string navigationProperty)
        {
            var itSystemUsage = Repository.GetByKey(key);
            if (itSystemUsage == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            switch (navigationProperty)
            {
                case "AccessTypes":
                    var accessTypeId = Convert.ToInt32(relatedKey);
                    var accessType = _accessTypeRepository.GetByKey(accessTypeId);

                    if (accessType == null)
                    {
                        return NotFound();
                    }
                    itSystemUsage.AccessTypes.Remove(accessType);
                    break;
                default:
                    return StatusCode(HttpStatusCode.NotImplemented);

            }
            Repository.Save();

            return StatusCode(HttpStatusCode.NoContent);
        }

    }
}
