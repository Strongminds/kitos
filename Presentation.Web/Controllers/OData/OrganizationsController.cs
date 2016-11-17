﻿using System;
using Core.ApplicationServices;
using Core.DomainModel.Organization;
using Core.DomainServices;
using System.Net;
using System.Security;
using System.Threading;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Core.DomainModel;

namespace Presentation.Web.Controllers.OData
{
    public class OrganizationsController : BaseEntityController<Organization>
    {
        private readonly IOrganizationService _organizationService;
        private readonly IOrganizationRoleService _organizationRoleService;
        private readonly IAuthenticationService _authService;
        private readonly IGenericRepository<User> _userRepository;

        public OrganizationsController(IGenericRepository<Organization> repository, IOrganizationService organizationService, IOrganizationRoleService organizationRoleService, IAuthenticationService authService, IGenericRepository<User> userRepository)
            : base(repository, authService)
        {
            _organizationService = organizationService;
            _organizationRoleService = organizationRoleService;
            _authService = authService;
            _userRepository = userRepository;
        }

        [ODataRoute("Organizations({orgKey})/RemoveUser")]
        public IHttpActionResult DeleteRemoveUserFromOrganization(int orgKey, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        [ODataRoute("Organizations({orgKey})/LastChangedByUser")]
        public virtual IHttpActionResult GetLastChangedByUser(int orgKey)
        {
            var loggedIntoOrgId = _authService.GetCurrentOrganizationId(UserId);
            if (loggedIntoOrgId != orgKey && !_authService.HasReadAccessOutsideContext(UserId))
                return StatusCode(HttpStatusCode.Forbidden);

            var result = Repository.GetByKey(orgKey).LastChangedByUser;
            return Ok(result);
        }

        [EnableQuery]
        [ODataRoute("Organizations({orgKey})/ObjectOwner")]
        public virtual IHttpActionResult GetObjectOwner(int orgKey)
        {
            var loggedIntoOrgId = _authService.GetCurrentOrganizationId(UserId);
            if (loggedIntoOrgId != orgKey && !_authService.HasReadAccessOutsideContext(UserId))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var result = Repository.GetByKey(orgKey).ObjectOwner;
            return Ok(result);
        }

        [EnableQuery]
        [ODataRoute("Organizations({orgKey})/Type")]
        public virtual IHttpActionResult GetType(int orgKey)
        {
            var loggedIntoOrgId = _authService.GetCurrentOrganizationId(UserId);
            if (loggedIntoOrgId != orgKey && !_authService.HasReadAccessOutsideContext(UserId))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var result = Repository.GetByKey(orgKey).Type;
            return Ok(result);
        }

        [EnableQuery]
        public override IHttpActionResult Post(Organization organization)
        {
            var loggedIntoOrgId = _authService.GetCurrentOrganizationId(UserId);
            if (loggedIntoOrgId != organization.Id && !_authService.HasReadAccessOutsideContext(UserId))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var user = _userRepository.GetByKey(UserId);

            CheckOrgTypeRights(organization);

            _organizationService.SetupDefaultOrganization(organization, user);

            var result = base.Post(organization).ExecuteAsync(new CancellationToken());

            if (result.Result.IsSuccessStatusCode && organization.TypeId == 2)
            {
                _organizationRoleService.MakeLocalAdmin(user, organization, user);
                _organizationRoleService.MakeUser(user, organization, user);
            }

            return Created(organization);
        }

        public override IHttpActionResult Patch(int key, Delta<Organization> delta)
        {
            var organization = delta.GetEntity();

            CheckOrgTypeRights(organization);
            return base.Patch(key, delta);
        }

        private void CheckOrgTypeRights(Organization organization)
        {
            if (organization.TypeId > 0)
            {
                var typeKey = (OrganizationTypeKeys) organization.TypeId;
                switch (typeKey)
                {
                    case OrganizationTypeKeys.Kommune:
                        if (!_authService.CanExecute(UserId, Feature.CanSetOrganizationTypeKommune))
                            throw new SecurityException();
                        break;
                    case OrganizationTypeKeys.Interessefællesskab:
                        if (!_authService.CanExecute(UserId, Feature.CanSetOrganizationTypeInteressefællesskab))
                            throw new SecurityException();
                        break;
                    case OrganizationTypeKeys.Virksomhed:
                        if (!_authService.CanExecute(UserId, Feature.CanSetOrganizationTypeVirksomhed))
                            throw new SecurityException();
                        break;
                    case OrganizationTypeKeys.AndenOffentligMyndighed:
                        if (!_authService.CanExecute(UserId, Feature.CanSetOrganizationTypeAndenOffentligMyndighed))
                            throw new SecurityException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
