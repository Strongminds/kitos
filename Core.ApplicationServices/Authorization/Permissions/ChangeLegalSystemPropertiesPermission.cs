using NotImplementedException = System.NotImplementedException;

namespace Core.ApplicationServices.Authorization.Permissions
{
    public class ChangeLegalSystemPropertiesPermission : Permission
    {
        public override bool Accept(IPermissionVisitor permissionVisitor)
        {
            permissionVisitor.Visit(this);
        }
    }
}
