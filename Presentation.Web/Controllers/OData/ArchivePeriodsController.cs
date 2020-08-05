﻿using System.Linq;
using System.Web.Http;
using Core.ApplicationServices.SystemUsage;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Infrastructure.Authorization.Controller.Crud;

namespace Presentation.Web.Controllers.OData
{
    [PublicApi]
    public class ArchivePeriodsController : BaseEntityController<ArchivePeriod>
    {
        private readonly IItSystemUsageService _itSystemUsageService;

        public ArchivePeriodsController(IGenericRepository<ArchivePeriod> repository, IItSystemUsageService itSystemUsageService)
            : base(repository)
        {
            _itSystemUsageService = itSystemUsageService;
        }

        protected override IControllerCrudAuthorization GetCrudAuthorization()
        {
            return new ChildEntityCrudAuthorization<ArchivePeriod, ItSystemUsage>(ap => _itSystemUsageService.GetById(ap.ItSystemUsageId), base.GetCrudAuthorization());
        }

        [RequireTopOnOdataThroughKitosToken]
        [EnableQuery]
        [ODataRoute("Organizations({organizationId})/ItSystemUsages({systemUsageId})/ArchivePeriods")]
        public IHttpActionResult GetArchivePeriodsForItSystemUsage(int organizationId, int systemUsageId)
        {
            if (GetOrganizationReadAccessLevel(organizationId) != OrganizationDataReadAccessLevel.All)
                return Forbidden();

            var itSystemUsage = _itSystemUsageService.GetById(systemUsageId);
            if (itSystemUsage == null)
            {
                return NotFound();
            }

            if (!CrudAuthorization.AllowRead(itSystemUsage))
            {
                return Forbidden();
            }

            return Ok(itSystemUsage.ArchivePeriods.AsQueryable());
        }
    }
}