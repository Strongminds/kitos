namespace Presentation.Web.Models.API.V2.Internal.Response.Notifications
{
    //TODO: Rename to NotificationResourcePermissionsDTO
    public class NotificationAccessRightsResponseDTO //TODO: Extend the common ResourceAccessRightsDTO
    {
        public bool CanBeDeleted { get; set; }
        public bool CanBeDeactivated { get; set; }
        public bool CanBeModified { get; set; }
    }
}