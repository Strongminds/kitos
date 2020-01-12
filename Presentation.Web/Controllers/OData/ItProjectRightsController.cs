﻿using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Core.DomainModel.ItProject;
using Core.DomainServices;
using Core.ApplicationServices;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.OData;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Net;

namespace Presentation.Web.Controllers.OData
{
    [PublicApi]
    public class ItProjectRightsController : BaseEntityController<ItProjectRight>
    {
        private readonly IAuthenticationService _authService;
        public ItProjectRightsController(IGenericRepository<ItProjectRight> repository, IAuthenticationService authService)
            : base(repository)
        {
            _authService = authService;
        }

        // GET /Users(1)/ItProjectRights
        [EnableQuery]
        [ODataRoute("Users({userId})/ItProjectRights")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ODataResponse<IQueryable<ItProjectRight>>))]
        public IHttpActionResult GetByUser(int userId)
        {
            // TODO figure out how to check auth
            var result = Repository.AsQueryable().Where(x => x.UserId == userId);
            return Ok(result);
        }

        public override IHttpActionResult Delete(int key)
        {
            var entity = Repository.GetByKey(key);

            if (entity == null)
                return NotFound();

            if (!_authService.HasWriteAccess(UserId, entity) && !_authService.IsLocalAdmin(this.UserId))
            {
                return Forbidden();
            }

            try
            {
                Repository.DeleteByKey(key);
                Repository.Save();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        public override IHttpActionResult Patch(int key, Delta<ItProjectRight> delta)
        {
            var entity = Repository.GetByKey(key);

            // does the entity exist?
            if (entity == null)
            {
                return NotFound();
            }

            // check if user is allowed to write to the entity
            if (!_authService.HasWriteAccess(UserId, entity) && !_authService.IsLocalAdmin(this.UserId))
            {
                return Forbidden();
            }

            // check model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // patch the entity
                delta.Patch(entity);
                Repository.Save();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }

            // add the request header "Prefer: return=representation"
            // if you want the updated entity returned,
            // else you'll just get 204 (No Content) returned
            return Updated(entity);
        }
    }
}
