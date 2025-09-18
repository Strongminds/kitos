using Presentation.Web.Models.API.V2.Response.Shared;

namespace Presentation.Web.Models.API.V2.Internal.Response.User
{
    public class UserCollectionPermissionsResponseDTO : ResourceCollectionPermissionsResponseDTO
    {
        public UserCollectionEditPermissionsResponseDTO Modify { get; set; }
        public bool Delete { get; set; }

        public UserCollectionPermissionsResponseDTO()
        {
            
        }

        public UserCollectionPermissionsResponseDTO(bool create, UserCollectionEditPermissionsResponseDTO modify, bool delete) : this()
        {
            Create = create;
            Modify = modify;
            Delete = delete;
        }
    }
}