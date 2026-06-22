using System;
using System.Linq;
using Core.DomainModel.Archive;
using Core.DomainServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V1.OData
{
    [InternalApi]
    public class ItSystemArchivesController : BaseEntityController<ItSystemArchive>
    {
        public ItSystemArchivesController(IGenericRepository<ItSystemArchive> repository)
            : base(repository)
        {
        }

        [EnableQuery]
        [Route("odata/ItSystemArchives")]
        [RequireTopOnOdataThroughKitosToken]
        public override IActionResult Get()
        {
            return base.Get();
        }

        protected override IQueryable<ItSystemArchive> GetAllQuery()
        {
            var all = base.GetAllQuery();
            if (UserContext.IsGlobalAdmin())
            {
                return all;
            }

            var orgIds = UserContext.OrganizationIds.ToList();
            return all.Where(x => orgIds.Contains(x.Organization.Id));
        }

        [NonAction]
        public override IActionResult Get(int key) => throw new NotSupportedException();

        [NonAction]
        public override IActionResult Post(int organizationId, ItSystemArchive entity) => throw new NotSupportedException();

        [NonAction]
        public override IActionResult Patch(int key, Delta<ItSystemArchive> delta) => throw new NotSupportedException();

        [NonAction]
        public override IActionResult Delete(int key) => throw new NotSupportedException();
    }
}
