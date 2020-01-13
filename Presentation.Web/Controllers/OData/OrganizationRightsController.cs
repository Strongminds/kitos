﻿using System;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Core.ApplicationServices.Model.Result;
using Core.ApplicationServices.Organizations;
using Core.DomainServices;
using Core.DomainModel.Organization;
using Core.DomainServices.Authorization;
using Core.DomainServices.Extensions;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData
{
    [InternalApi]
    public class OrganizationRightsController : BaseEntityController<OrganizationRight>
    {
        private readonly IOrganizationRightsService _organizationRightsService;

        public OrganizationRightsController(
            IGenericRepository<OrganizationRight> repository,
            IOrganizationRightsService organizationRightsService)
            : base(repository)
        {
            _organizationRightsService = organizationRightsService;
        }

        // GET /Organizations(1)/Rights
        [EnableQuery]
        [ODataRoute("Organizations({orgKey})/Rights")]
        public IHttpActionResult GetRights(int orgKey)
        {
            if (GetCrossOrganizationReadAccessLevel() != CrossOrganizationDataReadAccessLevel.All)
            {
                return Forbidden();
            }
            var result = Repository
                .AsQueryable()
                .ByOrganizationId(orgKey);

            return Ok(result);
        }

        // POST /Organizations(1)/Rights
        [ODataRoute("Organizations({orgKey})/Rights")]
        public IHttpActionResult PostRights(int orgKey, OrganizationRight entity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = _organizationRightsService.AddRightToUser(orgKey, entity);
                if (result.Ok)
                {
                    return Created(entity);
                }

                return result.Error == OperationFailure.Forbidden ?
                    Forbidden() :
                    InternalServerError();
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to add right", e);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Always Use 405 - POST /Organizations(orgKey)/Rights instead
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override IHttpActionResult Post(OrganizationRight entity)
        {
            return StatusCode(HttpStatusCode.MethodNotAllowed);
        }

        // DELETE /Organizations(1)/Rights(1)
        [ODataRoute("Organizations({orgKey})/Rights({key})")]
        public IHttpActionResult DeleteRights(int orgKey, int key)
        {
            return PerformDelete(key);
        }

        public override IHttpActionResult Delete(int key)
        {
            var entity = Repository.GetByKey(key);

            return entity == null ?
                NotFound() :
                PerformDelete(entity.Id);
        }

        private IHttpActionResult PerformDelete(int key)
        {
            try
            {
                var result = _organizationRightsService.RemoveRole(key);

                if (result.Ok)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }

                switch (result.Error)
                {
                    case OperationFailure.BadInput:
                        return BadRequest();
                    case OperationFailure.NotFound:
                        return NotFound();
                    case OperationFailure.Forbidden:
                        return Forbidden();
                    case OperationFailure.Conflict:
                        return StatusCode(HttpStatusCode.Conflict);
                    default:
                        return StatusCode(HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception e)
            {
                return StatusCode(HttpStatusCode.InternalServerError);
            }
        }
    }
}
