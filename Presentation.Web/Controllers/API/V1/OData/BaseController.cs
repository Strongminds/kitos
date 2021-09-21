﻿using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Microsoft.AspNet.OData;
using Core.DomainServices;
using Ninject;
using Ninject.Extensions.Logging;
using Core.ApplicationServices.Authorization;

namespace Presentation.Web.Controllers.API.V1.OData
{
    [Authorize]
    public abstract class BaseController<T> : ODataController where T : class
    {
        protected readonly IGenericRepository<T> Repository;

        [Inject]
        public IOrganizationalUserContext UserContext { get; set; }

        [Inject]
        public IAuthorizationContext AuthorizationContext { get; set; }

        [Inject]
        public ILogger Logger { get; set; }

        protected BaseController(IGenericRepository<T> repository)
        {
            Repository = repository;
        }

        protected int UserId => UserContext.UserId;

        [EnableQuery]
        public virtual IHttpActionResult Get()
        {
            return Ok(Repository.AsQueryable());
        }

        [EnableQuery(MaxExpansionDepth = 4)]
        public virtual IHttpActionResult Get(int key)
        {
            var entity = Repository.GetByKey(key);
            if (entity == null)
                return NotFound();

            return Ok(entity);
        }

        protected virtual ResponseMessageResult Forbidden()
        {
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Forbidden));
        }

    }
}
