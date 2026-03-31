using System.Net;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.DomainServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Presentation.Web.Extensions;

namespace Presentation.Web.Controllers.API.V1.OData
{
    [Authorize]
    public abstract class BaseController<T> : ODataController where T : class
    {
        protected readonly IGenericRepository<T> Repository;

        public IOrganizationalUserContext? UserContext { get; set; }

        public IAuthorizationContext? AuthorizationContext { get; set; }

        protected BaseController(IGenericRepository<T> repository)
        {
            Repository = repository;
        }

        protected int UserId => UserContext!.UserId;

        public virtual IActionResult Get()
        {
            return Ok(Repository.AsQueryable());
        }

        public virtual IActionResult Get(int key)
        {
            var entity = Repository.GetByKey(key);
            if (entity == null)
                return NotFound();

            return Ok(entity);
        }

        protected IActionResult Forbidden()
        {
            return StatusCode((int)HttpStatusCode.Forbidden);
        }
    }
}
