﻿using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Core.ApplicationServices.Project;
using Core.DomainModel.Constants;
using Core.DomainModel.ItProject;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Core.DomainServices.Repositories.Project;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Infrastructure.Authorization.Controller.Crud;
using Presentation.Web.Models.API.V1;

namespace Presentation.Web.Controllers.API.V1
{
    [PublicApi]
    public class HandoverController : GenericApiController<Handover, HandoverDTO>
    {
        private readonly IItProjectRepository _projectRepository;
        private readonly IItProjectService _projectService;

        public HandoverController(
            IGenericRepository<Handover> repository,
            IItProjectRepository projectRepository,
            IItProjectService projectService)
            : base(repository)
        {
            _projectRepository = projectRepository;
            _projectService = projectService;
        }

        protected override IControllerCrudAuthorization GetCrudAuthorization()
        {
            return new ChildEntityCrudAuthorization<Handover, ItProject>(goalStatus => _projectRepository.GetById(goalStatus.ItProject?.Id ?? EntityConstants.InvalidId), base.GetCrudAuthorization());
        }

        protected override IQueryable<Handover> GetAllQuery()
        {
            var query = Repository.AsQueryable();
            if (AuthorizationContext.GetCrossOrganizationReadAccess() == CrossOrganizationDataReadAccessLevel.All)
            {
                return query;
            }

            var organizationIds = UserContext.OrganizationIds.ToList();

            return query.Where(x => organizationIds.Contains(x.ItProject.OrganizationId));
        }

        [NonAction]
        public override HttpResponseMessage Delete(int id, int organizationId) => throw new NotSupportedException();

        [NonAction]
        public override HttpResponseMessage Post(int organizationId, HandoverDTO dto) => throw new NotSupportedException();

        public virtual HttpResponseMessage PostParticipant(int id, [FromUri] int participantId)
        {
            try
            {
                var handover = Repository.GetByKey(id);

                if (handover == null) return NotFound();

                var result = _projectService.AddHandoverParticipant(handover.ItProject?.Id ?? EntityConstants.InvalidId, participantId);

                return result.Ok ? 
                    Ok() : 
                    FromOperationFailure(result.Error);
            }
            catch (Exception e)
            {
                return LogError(e);
            }
        }

        public virtual HttpResponseMessage DeleteParticipant(int id, [FromUri] int participantId)
        {
            try
            {
                var handover = Repository.GetByKey(id);

                if (handover == null) return NotFound();

                var result = _projectService.DeleteHandoverParticipant(handover.ItProject?.Id ?? EntityConstants.InvalidId, participantId);

                return result.Ok ?
                    Ok() :
                    FromOperationFailure(result.Error);
            }

            catch (Exception e)
            {
                return LogError(e);
            }
        }
    }
}
