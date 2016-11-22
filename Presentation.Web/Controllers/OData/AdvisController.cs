﻿using Core.ApplicationServices;
using Core.DomainModel;
using Core.DomainModel.Advice;
using Core.DomainModel.AdviceSent;
using Core.DomainServices;
using Hangfire;
using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.OData;
using System.Web.OData.Routing;

namespace Presentation.Web.Controllers.OData
{
    public class AdvisController : BaseEntityController<AdviceSent>
    {

        IAuthenticationService _authService;
        public AdvisController(IGenericRepository<AdviceSent> repository, IAuthenticationService authService)
            : base(repository, authService)
        {
            _authService = authService;
        }

        [EnableQuery]
        public override IHttpActionResult Post(AdviceSent advis)
        {

            var response = base.Post(advis);

            if (response.GetType() == typeof(System.Web.OData.Results.CreatedODataResult<AdviceSent>)) {
                var server = new BackgroundJobServer();

                RecurringJob.AddOrUpdate(
                () => Console.WriteLine("Recurring!"),
                Cron.Daily);
                //TODO CREATE HANGFIRE SCHEDULE
            }

            return response;
        }

        [EnableQuery]
        public override IHttpActionResult Patch(int key, Delta<AdviceSent> delta)
        {
            var response = base.Patch(key, delta);

            if (response.GetType() == typeof(System.Web.OData.Results.UpdatedODataResult<AdviceSent>))
            {
                //TODO UPDATE HANGFIRE SCHEDULE
            }
            
            return response;
        }

        [EnableQuery]
        [ODataRoute("GetByObjectID(id={id},type={type})")]
        public IHttpActionResult GetByObjectID(int id,int type)
        {
            if (UserId == 0)
                return Unauthorized();

            var hasOrg = typeof(IHasOrganization).IsAssignableFrom(typeof(AdviceSent));

            if (_authService.HasReadAccessOutsideContext(UserId) || hasOrg == false)
                return Ok(Repository.AsQueryable().Where(x=> x.ObjectId == id && x.Type == (ObjectType)type));

            return Ok(Repository.AsQueryable().Where(x => ((IHasOrganization)x).OrganizationId == _authService.GetCurrentOrganizationId(UserId) && x.ObjectId == id && x.Type == (ObjectType)type));
        }

        [EnableQuery]
        public IHttpActionResult Delete(int ObjectId)
        {
            var response = base.Delete(ObjectId);

            if (response.GetType() == typeof(StatusCodeResult))
            {
                //TODO Delete HANGFIRE SCHEDULE
            }
            return response;
        }



    }
}