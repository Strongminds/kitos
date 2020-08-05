﻿using System;
using Core.DomainModel.Result;
using Core.DomainModel.SSO;
using Infrastructure.Services.Types;

namespace Core.DomainServices.Repositories.SSO
{
    public interface ISsoOrganizationIdentityRepository
    {
        Maybe<SsoOrganizationIdentity> GetByExternalUuid(Guid externalId);
        Result<SsoOrganizationIdentity, OperationError> AddNew(DomainModel.Organization.Organization organization, Guid externalId);
    }
}
