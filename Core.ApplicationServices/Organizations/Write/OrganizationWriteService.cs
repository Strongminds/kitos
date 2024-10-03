﻿using System;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.Organizations;
using Core.ApplicationServices.Model.Organizations.Write;
using Core.ApplicationServices.Model.Organizations.Write.MasterDataRoles;
using Core.DomainModel;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Generic;
using Core.DomainServices.Repositories.Organization;
using Infrastructure.Services.DataAccess;

namespace Core.ApplicationServices.Organizations.Write;

public class OrganizationWriteService : IOrganizationWriteService{
        
    private readonly ITransactionManager _transactionManager;               
    private readonly IDomainEvents _domainEvents;
    private readonly IOrganizationService _organizationService;
    private readonly IAuthorizationContext _authorizationContext;
    private readonly IOrganizationRepository _repository;
    private readonly IEntityIdentityResolver _identityResolver;
    private readonly IGenericRepository<ContactPerson> _contactPersonRepository;
    private readonly IGenericRepository<DataResponsible> _dataResponsibleRepository;
    private readonly IGenericRepository<DataProtectionAdvisor> _dataProtectionAdvisorRepository;


    public OrganizationWriteService(ITransactionManager transactionManager, IDomainEvents domainEvents, IOrganizationService organizationService, IAuthorizationContext authorizationContext, IOrganizationRepository repository, IEntityIdentityResolver identityResolver, IGenericRepository<ContactPerson> contactPersonRepository, IGenericRepository<DataResponsible> dataResponsibleRepository, IGenericRepository<DataProtectionAdvisor> dataProtectionAdvisorRepository)
    {
        _transactionManager = transactionManager;
        _domainEvents = domainEvents;
        _organizationService = organizationService;
        _authorizationContext = authorizationContext;
        _repository = repository;
        _identityResolver = identityResolver;
        _contactPersonRepository = contactPersonRepository;
        _dataResponsibleRepository = dataResponsibleRepository;
        _dataProtectionAdvisorRepository = dataProtectionAdvisorRepository;
    }

    public Result<Organization, OperationError> UpdateMasterData(Guid organizationUuid, OrganizationMasterDataUpdateParameters parameters)
    {
        using var transaction = _transactionManager.Begin();
        var result = _organizationService.GetOrganization(organizationUuid)
            .Bind(organization => Update(organization, parameters));

        if (result.Failed)
        {
            transaction.Rollback();
            return result;
        }

        transaction.Commit();
        return result;
    }

    private Result<Organization, OperationError> Update(Organization organization, OrganizationMasterDataUpdateParameters parameters)
    {
        var result = WithWriteAccess(organization)
            .Bind(organizationWithWriteAccess => PerformUpdates(organizationWithWriteAccess, parameters));

        if (result.Ok)
        {
            _domainEvents.Raise(new EntityUpdatedEvent<Organization>(result.Value));
            _repository.Update(result.Value);
        }
        return result;
    }

    private Result<Organization, OperationError> WithWriteAccess(Organization org)
    {
        return _authorizationContext.AllowModify(org) ? org : new OperationError(OperationFailure.Forbidden);
    }

    private Result<Organization, OperationError> PerformUpdates(Organization organization, OrganizationMasterDataUpdateParameters parameters)
    {
        return organization
            .WithOptionalUpdate(parameters.Address, UpdateOrganizationAddress)
            .Bind(org => org.WithOptionalUpdate(parameters.Cvr, UpdateOrganizationCvr))
            .Bind(org => org.WithOptionalUpdate(parameters.Email, UpdateOrganizationEmail))
            .Bind(org => org.WithOptionalUpdate(parameters.Phone, UpdateOrganizationPhone));
    }

    private Result<Organization, OperationError> UpdateOrganizationAddress(Organization organization,
        Maybe<string> address)
    {
        organization.Adress = address.HasValue ? address.Value : null;
        return organization;
    }
    private Result<Organization, OperationError> UpdateOrganizationCvr(Organization organization,
        Maybe<string> cvr)
    {
        organization.Cvr = cvr.HasValue ? cvr.Value : null;
        return organization;
    }
    private Result<Organization, OperationError> UpdateOrganizationEmail(Organization organization,
        Maybe<string> email)
    {
        organization.Email = email.HasValue ? email.Value : null;
        return organization;
    }
    private Result<Organization, OperationError> UpdateOrganizationPhone(Organization organization,
        Maybe<string> phone)
    {
        organization.Phone = phone.HasValue ? phone.Value : null;
        return organization;
    }

    public Result<OrganizationMasterDataRoles, OperationError> GetOrCreateOrganizationMasterDataRoles(Guid organizationUuid)
    {
        using var transaction = _transactionManager.Begin();

        var result = AuthorizeAndPerformMasterDataRolesGetOrCreate(organizationUuid);

        if (result.Failed)
        {
            transaction.Rollback();
            return result;
        }

        transaction.Commit();
        return result;
    }

    private Result<OrganizationMasterDataRoles, OperationError> AuthorizeAndPerformMasterDataRolesGetOrCreate(
        Guid organizationUuid)
    {
        var organizationDbIdMaybe = _identityResolver.ResolveDbId<Organization>(organizationUuid);
        if (organizationDbIdMaybe.IsNone) return new OperationError(OperationFailure.BadInput);
        var orgId = organizationDbIdMaybe.Value;

        var contactPersonResult = _organizationService.GetContactPerson(orgId)
            .Match(cp => cp,
                () => AuthorizeCreationAndCreateContactPerson(orgId));
        if (contactPersonResult.Failed) return contactPersonResult.Error;

        var dataResponsibleResult = _organizationService.GetDataResponsible(orgId)
            .Match(dr => dr,
                () => AuthorizeCreationAndCreateDataResponsible(orgId));
        if (dataResponsibleResult.Failed) return dataResponsibleResult.Error;

        var dataProtectionAdvisorResult = _organizationService.GetDataProtectionAdvisor(orgId)
            .Match(dpa => dpa,
                () => AuthorizeCreationAndCreateDataProtectionAdvisor(orgId));
        if (dataProtectionAdvisorResult.Failed) return dataProtectionAdvisorResult.Error;

        return new OrganizationMasterDataRoles
        {
            OrganizationUuid = organizationUuid,
            ContactPerson = contactPersonResult.Value,
            DataResponsible = dataResponsibleResult.Value,
            DataProtectionAdvisor = dataProtectionAdvisorResult.Value
        };
    }

    public Result<OrganizationMasterDataRoles, OperationError> UpsertOrganizationMasterDataRoles(Guid organizationUuid,
        OrganizationMasterDataRolesUpdateParameters updateParameters)
    {
        using var transaction = _transactionManager.Begin();

        var result = AuthorizeAndPerformMasterDataRolesUpsert(organizationUuid, updateParameters);

        if (result.Failed)
        {
            transaction.Rollback();
            return result;
        }

        transaction.Commit();
        return result;
    }

    private Result<OrganizationMasterDataRoles, OperationError> AuthorizeAndPerformMasterDataRolesUpsert(Guid organizationUuid,
        OrganizationMasterDataRolesUpdateParameters updateParameters)
    {
        var organizationResult = _organizationService.GetOrganization(organizationUuid);
        if (organizationResult.Failed) return new OperationError(OperationFailure.BadInput);

        var organization = organizationResult.Value;

        var modifiedContactPersonResult =
            AuthorizeModificationAndUpsertContactPerson(organization, updateParameters.ContactPerson);
        if (modifiedContactPersonResult.Failed) return modifiedContactPersonResult.Error;

        var modifiedDataResponsibleResult =
            AuthorizeModificationAndUpsertDataResponsible(organization, updateParameters.DataResponsible);
        if (modifiedDataResponsibleResult.Failed) return modifiedDataResponsibleResult.Error;

        var modifiedDataProtectionAdvisorResult = AuthorizeModificationAndUpsertDataProtectionAdvisor(organization,
            updateParameters.DataProtectionAdvisor);
        if (modifiedDataProtectionAdvisorResult.Failed) return modifiedDataProtectionAdvisorResult.Error;

        return new OrganizationMasterDataRoles()
        {
            OrganizationUuid = organizationUuid,
            ContactPerson = modifiedContactPersonResult.Value,
            DataProtectionAdvisor = modifiedDataProtectionAdvisorResult.Value,
            DataResponsible = modifiedDataResponsibleResult.Value
        };
    }

    private Result<ContactPerson, OperationError> AuthorizeModificationAndUpsertContactPerson(
        Organization organization, Maybe<ContactPersonUpdateParameters> parameters)
    {
        return UpsertContactPerson(organization.Id)
            .Bind(contactPerson => ValidateModifyContactPersonByRootOrganization(contactPerson, organization))
            .Bind(contactPerson => ModifyContactPerson(contactPerson, parameters));
    }
    
    private Result<ContactPerson, OperationError> UpsertContactPerson(int organizationId)
        {
            return _organizationService.GetContactPerson(organizationId)
                .Match(contactPerson => contactPerson,
                    () => AuthorizeCreationAndCreateContactPerson(organizationId));
        }

    private Result<ContactPerson, OperationError> ValidateModifyContactPersonByRootOrganization(ContactPerson contactPerson, Organization organization) =>
        _authorizationContext.AllowModify(organization) ? contactPerson : new OperationError(OperationFailure.Forbidden);

    private Result<ContactPerson, OperationError> AuthorizeCreationAndCreateContactPerson(int orgId)
    {
        return ValidateCreateContactPerson(orgId).Bind(SaveContactPerson);
    }

    private Result<ContactPerson, OperationError> ValidateCreateContactPerson(int orgId) =>
        _authorizationContext.AllowCreate<ContactPerson>(orgId) ? new ContactPerson() { OrganizationId = orgId } : new OperationError(OperationFailure.Forbidden);

    private Result<ContactPerson, OperationError> SaveContactPerson(ContactPerson contactPerson)
    {
        _contactPersonRepository.Insert(contactPerson);
        _domainEvents.Raise(new EntityCreatedEvent<ContactPerson>(contactPerson));
        _contactPersonRepository.Save();
        return contactPerson;
    }

    private Result<ContactPerson, OperationError> ModifyContactPerson(ContactPerson contactPerson, Maybe<ContactPersonUpdateParameters> parametersMaybe)
    {
        if (parametersMaybe.IsNone)
            return contactPerson;

        var parameters = parametersMaybe.Value;

        var emailParam = parameters.Email;
        if (emailParam != null && emailParam.HasChange) contactPerson.Email = emailParam.NewValue.HasValue ? emailParam.NewValue.Value : null;

        var nameParam = parameters.Name;
        if (nameParam != null && nameParam.HasChange) contactPerson.Name = nameParam.NewValue.HasValue ? nameParam.NewValue.Value : null;

        var lastNameParam = parameters.LastName;
        if (lastNameParam != null && lastNameParam.HasChange) contactPerson.LastName = lastNameParam.NewValue.HasValue ? lastNameParam.NewValue.Value : null;

        var phoneNumberParam = parameters.PhoneNumber;
        if (phoneNumberParam != null && phoneNumberParam.HasChange)
            contactPerson.PhoneNumber = phoneNumberParam.NewValue.HasValue ? phoneNumberParam.NewValue.Value : null;
        
        _contactPersonRepository.Update(contactPerson);
        _domainEvents.Raise(new EntityUpdatedEvent<ContactPerson>(contactPerson));
        _contactPersonRepository.Save();

        return contactPerson;
    }

    private Result<DataResponsible, OperationError> AuthorizeModificationAndUpsertDataResponsible(
        Organization organization, Maybe<DataResponsibleUpdateParameters> parameters)
    {
        return UpsertDataResponsible(organization.Id)
            .Bind(dataResponsible => ValidateModifyDataResponsibleByRootOrganization(dataResponsible, organization))
            .Bind(dataResponsible => ModifyDataResponsible(dataResponsible, parameters));
    }
    private Result<DataResponsible, OperationError> ValidateModifyDataResponsibleByRootOrganization(DataResponsible dataResponsible, Organization organization) =>
        _authorizationContext.AllowModify(organization) ? dataResponsible : new OperationError(OperationFailure.Forbidden);


    private Result<DataResponsible, OperationError> UpsertDataResponsible(int orgId)
    {
        return _organizationService.GetDataResponsible(orgId)
            .Match(dr => dr,
                () => AuthorizeCreationAndCreateDataResponsible(orgId));
    }

    private Result<DataResponsible, OperationError> AuthorizeCreationAndCreateDataResponsible(int orgId)
    {
       return ValidateCreateDataResponsible(orgId).Bind(SaveDataResponsible);
    }

    private Result<DataResponsible, OperationError> ValidateCreateDataResponsible(int orgId) =>
        _authorizationContext.AllowCreate<DataResponsible>(orgId) ? new DataResponsible() { OrganizationId = orgId } : new OperationError(OperationFailure.Forbidden);

    private Result<DataResponsible, OperationError> SaveDataResponsible(DataResponsible dataResponsible)
    {
        _dataResponsibleRepository.Insert(dataResponsible);
        _domainEvents.Raise(new EntityCreatedEvent<DataResponsible>(dataResponsible));
        _dataResponsibleRepository.Save();
        return dataResponsible;
    }

    private Result<DataResponsible, OperationError> ModifyDataResponsible(DataResponsible dataResponsible, Maybe<DataResponsibleUpdateParameters> parametersMaybe)
    {
        if (parametersMaybe.IsNone)
            return dataResponsible;

        var parameters = parametersMaybe.Value;

        var emailParam = parameters.Email;
        if (emailParam != null && emailParam.HasChange) dataResponsible.Email = emailParam.NewValue.HasValue ? emailParam.NewValue.Value : null;

        var nameParam = parameters.Name;
        if (nameParam != null && nameParam.HasChange) dataResponsible.Name = nameParam.NewValue.HasValue ? nameParam.NewValue.Value : null;

        var cvrParam = parameters.Cvr;
        if (cvrParam != null && cvrParam.HasChange) dataResponsible.Cvr = cvrParam.NewValue.HasValue ? cvrParam.NewValue.Value : null;

        var phoneParam = parameters.Phone;
        if (phoneParam != null && phoneParam.HasChange)
            dataResponsible.Phone = phoneParam.NewValue.HasValue ? phoneParam.NewValue.Value : null;

        var addressParam = parameters.Address;
        if (addressParam != null && addressParam.HasChange)
            dataResponsible.Adress = addressParam.NewValue.HasValue ? addressParam.NewValue.Value : null;

        _dataResponsibleRepository.Update(dataResponsible);
        _domainEvents.Raise(new EntityUpdatedEvent<DataResponsible>(dataResponsible));
        _dataResponsibleRepository.Save();

        return dataResponsible;
    }

    private Result<DataProtectionAdvisor, OperationError> AuthorizeModificationAndUpsertDataProtectionAdvisor(
        Organization organization, Maybe<DataProtectionAdvisorUpdateParameters> parameters)
    {
        return UpsertDataProtectionAdvisor(organization.Id)
            .Bind(dataProtectionAdvisor => ValidateModifyDataProtectionAdvisorByRootOrganization(dataProtectionAdvisor, organization))
            .Bind(dataProtectionAdvisor => ModifyDataProtectionAdvisor(dataProtectionAdvisor, parameters));
    }

    private Result<DataProtectionAdvisor, OperationError> UpsertDataProtectionAdvisor(int orgId)
    {
        return _organizationService.GetDataProtectionAdvisor(orgId)
            .Match(dataProtectionAdvisor => dataProtectionAdvisor,
                () => AuthorizeCreationAndCreateDataProtectionAdvisor(orgId));
    }
    private Result<DataProtectionAdvisor, OperationError> AuthorizeCreationAndCreateDataProtectionAdvisor(int orgId)
    {
        return ValidateCreateDataProtectionAdvisor(orgId).Bind(SaveDataProtectionAdvisor);
    }

    private Result<DataProtectionAdvisor, OperationError> ValidateCreateDataProtectionAdvisor(int orgId) =>
        _authorizationContext.AllowCreate<DataProtectionAdvisor>(orgId) ? new DataProtectionAdvisor() { OrganizationId = orgId } : new OperationError(OperationFailure.Forbidden);

    private Result<DataProtectionAdvisor, OperationError> SaveDataProtectionAdvisor(DataProtectionAdvisor dataProtectionAdvisor)
    {
        _dataProtectionAdvisorRepository.Insert(dataProtectionAdvisor);
        _domainEvents.Raise(new EntityCreatedEvent<DataProtectionAdvisor>(dataProtectionAdvisor));
        _dataProtectionAdvisorRepository.Save();
        return dataProtectionAdvisor;
    }

    private Result<DataProtectionAdvisor, OperationError> ValidateModifyDataProtectionAdvisorByRootOrganization(DataProtectionAdvisor dataProtectionAdvisor, Organization organization) =>
        _authorizationContext.AllowModify(organization) ? dataProtectionAdvisor : new OperationError(OperationFailure.Forbidden);

    private Result<DataProtectionAdvisor, OperationError> ModifyDataProtectionAdvisor(DataProtectionAdvisor dataProtectionAdvisor, Maybe<DataProtectionAdvisorUpdateParameters> parametersMaybe)
    {
        if (parametersMaybe.IsNone)
            return dataProtectionAdvisor;

        var parameters = parametersMaybe.Value;

        var emailParam = parameters.Email;
        if (emailParam != null && emailParam.HasChange) dataProtectionAdvisor.Email = emailParam.NewValue.HasValue ? emailParam.NewValue.Value : null;

        var nameParam = parameters.Name;
        if (nameParam != null && nameParam.HasChange) dataProtectionAdvisor.Name = nameParam.NewValue.HasValue ? nameParam.NewValue.Value : null;

        var cvrParam = parameters.Cvr;
        if (cvrParam != null && cvrParam.HasChange) dataProtectionAdvisor.Cvr = cvrParam.NewValue.HasValue ? cvrParam.NewValue.Value : null;

        var phoneParam = parameters.Phone;
        if (phoneParam != null && phoneParam.HasChange)
            dataProtectionAdvisor.Phone = phoneParam.NewValue.HasValue ? phoneParam.NewValue.Value : null;

        var addressParam = parameters.Address;
        if (addressParam != null && addressParam.HasChange)
            dataProtectionAdvisor.Adress = addressParam.NewValue.HasValue ? addressParam.NewValue.Value : null;

        _dataProtectionAdvisorRepository.Update(dataProtectionAdvisor);
        _domainEvents.Raise(new EntityUpdatedEvent<DataProtectionAdvisor>(dataProtectionAdvisor));
        _dataProtectionAdvisorRepository.Save();

        return dataProtectionAdvisor;
    }
}