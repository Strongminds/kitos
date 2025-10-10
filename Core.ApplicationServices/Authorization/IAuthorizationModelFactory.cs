namespace Core.ApplicationServices.Authorization
{
    public interface IAuthorizationModelFactory
    {
        CrudAuthorizationModel CreateCrudAuthorizationModel();
        FieldAuthorizationModel CreateFieldAuthorizationModel();
    }
}
