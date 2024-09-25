﻿using System.Linq;
using Core.DomainModel;
using Presentation.Web.Models.API.V2.Internal.Response.User;

namespace Presentation.Web.Controllers.API.V2.Internal.Users.Mapping
{
    public class UserResponseModelMapper : IUserResponseModelMapper
    {
        public UserResponseDTO ToUserResponseDTO(User user)
        {
            return new UserResponseDTO
            {
                Uuid = user.Uuid,
                Email = user.Email,
                FirstName = user.Name,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DefaultUserStartPreference =
                    DefaultUserStartPreferenceChoiceMapper.GetDefaultUserStartPreferenceChoice(
                        user.DefaultUserStartPreference),
                HasApiAccess = user.HasApiAccess,
                HasStakeHolderAccess = user.HasStakeHolderAccess,
                Roles = user.OrganizationRights.Select(x => x.Role.ToOrganizationRoleChoice()).ToList(),
            };
        }
    }
}