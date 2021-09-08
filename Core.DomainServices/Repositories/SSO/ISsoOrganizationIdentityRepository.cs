﻿using System;
using Core.Abstractions.Types;
using Core.DomainModel.SSO;


namespace Core.DomainServices.Repositories.SSO
{
    public interface ISsoOrganizationIdentityRepository
    {
        Maybe<SsoOrganizationIdentity> GetByExternalUuid(Guid externalId);
        Result<SsoOrganizationIdentity, OperationError> AddNew(DomainModel.Organization.Organization organization, Guid externalId);
    }
}
