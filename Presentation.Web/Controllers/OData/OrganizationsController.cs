﻿using Core.DomainModel.Organization;
using Core.DomainServices;
using System.Net;
using System.Security;
using System.Threading;
using System.Web.Http;
using System.Web.OData;
using Core.DomainModel;
using System.Linq;
using Core.DomainServices.Authorization;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData
{
    [InternalApi]
    public class OrganizationsController : BaseEntityController<Organization>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IOrganizationRoleService _organizationRoleService;
        private readonly IGenericRepository<User> _userRepository;

        public OrganizationsController(
            IGenericRepository<Organization> repository,
            IOrganizationService organizationService,
            IOrganizationRoleService organizationRoleService,
            IGenericRepository<User> userRepository)
            : base(repository)
        {
            _organizationService = organizationService;
            _organizationRoleService = organizationRoleService;
            _userRepository = userRepository;
        }

        [HttpPost]
        public IHttpActionResult RemoveUser([FromODataUri]int orgKey, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = Repository.GetByKey(orgKey);
            if (entity == null)
            {
                return NotFound();
            }

            if (!AllowWrite(entity))
            {
                return Forbidden();
            }

            var userId = 0;
            if (parameters.ContainsKey("userId"))
            {
                userId = (int)parameters["userId"];
                // TODO check if user is allowed to remove users from this organization
            }

            _organizationService.RemoveUser(orgKey, userId);

            return StatusCode(HttpStatusCode.NoContent);
        }

        [EnableQuery]
        public override IHttpActionResult Post(Organization organization)
        {
            if (organization == null)
            {
                return BadRequest();
            }

            if (IsCvrInvalid(organization))
            {
                return BadRequest("Invalid CVR format");
            }

            if (!AllowCreate<Organization>(organization))
            {
                return Forbidden();
            }

            var user = _userRepository.GetByKey(UserId);

            _organizationService.SetupDefaultOrganization(organization, user);

            var result = base.Post(organization).ExecuteAsync(new CancellationToken());

            if (result.Result.IsSuccessStatusCode)
            {
                if (organization.TypeId == (int)OrganizationTypeKeys.Interessefællesskab)
                {
                    _organizationRoleService.MakeLocalAdmin(user, organization, user);
                    _organizationRoleService.MakeUser(user, organization, user);
                }
            }
            else
            {
                return StatusCode(result.Result.StatusCode);
            }

            return Created(organization);
        }

        private static bool IsCvrInvalid(Organization organization)
        {
            //Cvr is optional
            var isCvrProvided = string.IsNullOrWhiteSpace(organization.Cvr) == false;

            //If cvr is defined, it must be valid
            return isCvrProvided && (organization.Cvr.Length > 10 || organization.Cvr.Length < 8);
        }

        [EnableQuery]
        public IHttpActionResult GetUsers([FromODataUri] int key)
        {
            var accessLevel = GetOrganizationReadAccessLevel(key);
            if (accessLevel < OrganizationDataReadAccessLevel.Public)
            {
                return Forbidden();
            }

            var result = _userRepository.AsQueryable().Where(m => m.OrganizationRights.Any(r => r.OrganizationId == key));
            return Ok(result);
        }

        public override IHttpActionResult Patch(int key, Delta<Organization> delta)
        {

            try
            {
                var organization = delta.GetInstance();
                if (organization.TypeId > 0)
                {
                    var typeKey = (OrganizationTypeKeys)organization.TypeId;
                    if (!_organizationService.CanCreateOrganizationOfType(organization, typeKey))
                    {
                        return Forbidden();
                    }
                }
            }
            catch (SecurityException e)
            {
                return Forbidden();
            }
            return base.Patch(key, delta);
        }
    }
}
