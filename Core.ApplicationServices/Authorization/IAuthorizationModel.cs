using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel.GDPR;

namespace Core.ApplicationServices.Authorization
{
    public interface IAuthorizationModel
    {
        bool AuthorizeUpdate(DataProcessingRegistration entity,
            DataProcessingRegistrationModificationParameters parameters);
    }
}
