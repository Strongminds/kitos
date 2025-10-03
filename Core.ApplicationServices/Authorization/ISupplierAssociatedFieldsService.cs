using Core.ApplicationServices.Model.GDPR.Write;

namespace Core.ApplicationServices.Authorization
{
    public interface ISupplierAssociatedFieldsService
    {
        bool RequestsChangesToSupplierAssociatedFields(DataProcessingRegistrationModificationParameters parameters);
        bool RequestsChangesToNonSupplierAssociatedFields(DataProcessingRegistrationModificationParameters parameters);
    }
}
