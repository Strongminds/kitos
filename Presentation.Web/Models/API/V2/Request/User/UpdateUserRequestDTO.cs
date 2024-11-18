using System;

namespace Presentation.Web.Models.API.V2.Request.User
{
    public class UpdateUserRequestDTO : BaseUserRequestDTO
    {
        public Guid OrganizationUnitUuid { get; set; }
    }
}