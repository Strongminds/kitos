using System;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel.GDPR;

namespace Core.ApplicationServices.Authorization;

public class FieldAuthorizationModel : IAuthorizationModel
{
    public bool AuthorizeUpdate(DataProcessingRegistration entity, DataProcessingRegistrationModificationParameters parameters)
    {
        throw new NotImplementedException();
    }
}