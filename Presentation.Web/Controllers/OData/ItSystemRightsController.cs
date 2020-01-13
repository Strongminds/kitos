﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Core.DomainModel.ItSystem;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.OData;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using Core.ApplicationServices.SystemUsage;
using Presentation.Web.Infrastructure.Authorization.Controller.Crud;

namespace Presentation.Web.Controllers.OData
{
    [PublicApi]
    public class ItSystemRightsController : BaseEntityController<ItSystemRight>
    {
        private readonly IItSystemUsageService _systemUsageService;

        public ItSystemRightsController(
            IGenericRepository<ItSystemRight> repository,
            IItSystemUsageService systemUsageService)
            : base(repository)
        {
            _systemUsageService = systemUsageService;
        }

        protected override IControllerCrudAuthorization GetCrudAuthorization()
        {
            return new ChildEntityCrudAuthorization<ItSystemRight>(sr => _systemUsageService.GetById(sr.ObjectId), base.GetCrudAuthorization());
        }

        // GET /Users(1)/ItProjectRights
        [EnableQuery]
        [ODataRoute("Users({userId})/ItSystemRights")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IQueryable<ItSystemRight>>))]
        public IHttpActionResult GetByUser(int userId)
        {
            var result = Repository.AsQueryable().Where(x => x.UserId == userId).ToList();

            result = FilterByAccessControl(result);

            return Ok(result.AsQueryable());
        }

        private List<ItSystemRight> FilterByAccessControl(List<ItSystemRight> result)
        {
            result = result.Where(AllowRead).ToList();
            return result;
        }
    }
}
