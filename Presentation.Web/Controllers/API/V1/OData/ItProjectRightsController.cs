﻿using System.Linq;
using System.Web.Http;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Core.DomainModel.ItProject;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.OData;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using Core.DomainModel.Events;
using Core.DomainServices.Repositories.Project;
using Presentation.Web.Infrastructure.Authorization.Controller.Crud;

namespace Presentation.Web.Controllers.API.V1.OData
{
    [PublicApi]
    public class ItProjectRightsController : BaseEntityController<ItProjectRight>
    {
        private readonly IItProjectRepository _itProjectRepository;

        public ItProjectRightsController(IGenericRepository<ItProjectRight> repository, IItProjectRepository itProjectRepository)
            : base(repository)
        {
            _itProjectRepository = itProjectRepository;
        }

        protected override IControllerCrudAuthorization GetCrudAuthorization()
        {
            return new ChildEntityCrudAuthorization<ItProjectRight, ItProject>(r => _itProjectRepository.GetById(r.ObjectId), base.GetCrudAuthorization());
        }

        // GET /Users(1)/ItProjectRights
        [EnableQuery]
        [ODataRoute("Users({userId})/ItProjectRights")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IQueryable<ItProjectRight>>))]
        [RequireTopOnOdataThroughKitosToken]
        public IHttpActionResult GetByUser(int userId)
        {
            var result = GetAllQuery().Where(x => x.UserId == userId);

            return Ok(result);
        }

        protected override IQueryable<ItProjectRight> GetAllQuery()
        {
            var all = base.GetAllQuery();
            if (UserContext.IsGlobalAdmin())
                return all;
            var orgIds = UserContext.OrganizationIds.ToList();
            return all.Where(x => orgIds.Contains(x.Object.OrganizationId));
        }

        protected override void RaiseCreatedDomainEvent(ItProjectRight entity)
        {
            base.RaiseCreatedDomainEvent(entity);
            RaiseRootUpdated(entity);
        }

        protected override void RaiseDeletedDomainEvent(ItProjectRight entity)
        {
            base.RaiseDeletedDomainEvent(entity);
            RaiseRootUpdated(entity);
        }

        protected override void RaiseUpdatedDomainEvent(ItProjectRight entity)
        {
            base.RaiseUpdatedDomainEvent(entity);
            RaiseRootUpdated(entity);
        }

        private void RaiseRootUpdated(ItProjectRight entity)
        {
            var root = entity.Object ?? _itProjectRepository.GetById(entity.ObjectId);
            if (root != null)
                DomainEvents.Raise(new EntityUpdatedEvent<ItProject>(root));
        }
    }
}
