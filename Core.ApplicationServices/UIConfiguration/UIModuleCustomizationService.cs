﻿using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.UiCustomization;
using Core.ApplicationServices.Organizations;
using Core.DomainModel.Organization;
using Core.DomainModel.UIConfiguration;
using Core.DomainServices.Authorization;
using Core.DomainServices.Generic;
using Core.DomainServices.Repositories.UICustomization;
using Infrastructure.Services.DataAccess;

namespace Core.ApplicationServices.UIConfiguration
{
    public class UIModuleCustomizationService : IUIModuleCustomizationService
    {
        private readonly ITransactionManager _transactionManager;
        private readonly IOrganizationalUserContext _userContext;
        private readonly IOrganizationService _organizationService;
        private readonly IEntityIdentityResolver _identityResolver;
        private readonly IUIModuleCustomizationRepository _repository;

        public UIModuleCustomizationService(ITransactionManager transactionManager,
            IOrganizationalUserContext userContext,
            IOrganizationService organizationService,
            IEntityIdentityResolver identityResolver,
            IUIModuleCustomizationRepository repository)
        {
            _transactionManager = transactionManager;
            _userContext = userContext;
            _organizationService = organizationService;
            _identityResolver = identityResolver;
            _repository = repository;
        }


        public Result<UIModuleCustomization, OperationError> GetModuleConfigurationForOrganization(int organizationId, string module)
        {
            return GetOrganizationById(organizationId)
                .Bind(organization => organization.GetUiModuleCustomization(module)
                    .Match(
                        Result<UIModuleCustomization, OperationError>.Success,
                        () => new OperationError($"module customization {module} not found on organization with id:{organizationId}", OperationFailure.NotFound)
                    )
                );
        }

        public Maybe<OperationError> UpdateModule(UIModuleCustomizationParameters parameters)
        {
            return GetOrganizationById(parameters.OrganizationId)
                .Match(organization =>
                    {
                        //using var transaction = _transactionManager.Begin();
                        if (!_userContext.HasRole(parameters.OrganizationId, OrganizationRole.LocalAdmin))
                            return new OperationError("User is not a local admin in organization",OperationFailure.Forbidden);

                        var result = organization.ModifyModuleCustomization(parameters.Module,
                            MapNodeParametersToCustomizedUiNodes(parameters.Nodes));
                        if (result.Failed)
                            return result.Error;
                        
                        _repository.Update(organization, result.Value);
                        //transaction.Commit();

                        return Maybe<OperationError>.None;
                    },
                    error => error
                );
        }

        private Result<Organization, OperationError> GetOrganizationById(int organizationId)
        {
            return _identityResolver
                .ResolveUuid<Organization>(organizationId)
                .Select(uuid => _organizationService.GetOrganization(uuid, OrganizationDataReadAccessLevel.All))
                .Match(
                    result => result,
                    () => Result<Organization, OperationError>.Failure(new OperationError(
                    $"Organization uuid could not be resolved from id:{organizationId}", OperationFailure.NotFound))
                    );
        }

        private static IEnumerable<CustomizedUINode> MapNodeParametersToCustomizedUiNodes(IEnumerable<CustomUINodeParameters> parameters)
        {
            return parameters.Select(x => new CustomizedUINode() {Key = x.Key, Enabled = x.Enabled});
        }
    }
}
