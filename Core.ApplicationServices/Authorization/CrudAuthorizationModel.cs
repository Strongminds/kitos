using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel.GDPR;

namespace Core.ApplicationServices.Authorization;

public class CrudAuthorizationModel(IAuthorizationContext authorizationContext) : IAuthorizationModel
{
    private IAuthorizationContext _authorizationContext = authorizationContext;

    public bool AuthorizeUpdate(DataProcessingRegistration entity, DataProcessingRegistrationModificationParameters parameters)
    {
        return _authorizationContext.AllowModify(entity);
    }
}