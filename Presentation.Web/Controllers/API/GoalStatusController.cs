﻿using Core.DomainModel;
using Core.DomainModel.Constants;
using Core.DomainModel.ItProject;
using Core.DomainServices;
using Core.DomainServices.Repositories.Project;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Infrastructure.Authorization.Controller.Crud;
using Presentation.Web.Models;

namespace Presentation.Web.Controllers.API
{
    [PublicApi]
    public class GoalStatusController : GenericApiController<GoalStatus, GoalStatusDTO>
    {
        private readonly IItProjectRepository _projectRepository;

        public GoalStatusController(
            IGenericRepository<GoalStatus> repository,
            IItProjectRepository projectRepository
            )
            : base(repository)
        {
            _projectRepository = projectRepository;
        }

        protected override IControllerCrudAuthorization GetCrudAuthorization()
        {
            return new ChildEntityCrudAuthorization<GoalStatus, ItProject>(goalStatus => _projectRepository.GetById(goalStatus.ItProject?.Id ?? EntityConstants.InvalidId), base.GetCrudAuthorization());
        }
    }
}
