using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Web.Models.API.V2.Request.User
{
    public class CreateUserRequestDTO
    {
        [Required]
        [EmailAddress]
        public new string Email { get; set; }

        [Required]
        public new string FirstName { get; set; }

        [Required]
        public new string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DefaultUserStartPreferenceChoice DefaultUserStartPreference { get; set; }
        public bool HasApiAccess { get; set; }
        public bool HasStakeHolderAccess { get; set; }
        public IEnumerable<OrganizationRoleChoice> Roles { get; set; }
        public bool SendMail { get; set; }

    }
}