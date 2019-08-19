﻿using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Core.DomainModel.ItSystem;
using Core.DomainServices;
using Core.ApplicationServices;
using Presentation.Web.Access;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData
{
    [PublicApi]
    public class ItSystemsController : BaseEntityController<ItSystem>
    {
        public ItSystemsController(IGenericRepository<ItSystem> repository, IAuthenticationService authService, IAccessContext accessContext)
            : base(repository, authService, accessContext)
        {
        }

        // GET /Organizations(1)/ItSystems
        [EnableQuery]
        [ODataRoute("Organizations({orgKey})/ItSystems")]
        public IHttpActionResult GetItSystems(int orgKey)
        {
            if (!AllowOrganizationAccess(orgKey))
            { 
                return Forbidden();
            }

            var result = Repository.AsQueryable().Where(m => m.OrganizationId == orgKey);

            var systemsWithAllowedReadAccess  = result.AsEnumerable().Where(AllowReadAccess);

            return Ok(systemsWithAllowedReadAccess);
        }

        // GET /Organizations(1)/ItSystems(1)
        [EnableQuery]
        [ODataRoute("Organizations({orgKey})/ItSystems({sysKey})")]
        public IHttpActionResult GetItSystems(int orgKey, int sysKey)
        {
            if (!AllowOrganizationAccess(orgKey))
            {
                return Forbidden();
            }

            var system = Repository.AsQueryable().SingleOrDefault(m => m.Id == sysKey);
            if (system == null)
            {
                return NotFound();
            }

            return Ok(system);
        }

        [ODataRoute("ItSystems")]
        public override IHttpActionResult Get()
        {
            return base.Get();
        }
    }
}
