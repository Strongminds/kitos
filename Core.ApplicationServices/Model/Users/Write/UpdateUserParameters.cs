﻿using System;
using System.Collections.Generic;
using Core.ApplicationServices.Model.Shared;
using Core.DomainModel.Organization;

namespace Core.ApplicationServices.Model.Users.Write
{
    public class UpdateUserParameters
    {
        public OptionalValueChange<string> Email { get; set; }
        public OptionalValueChange<string> FirstName { get; set; }
        
        public OptionalValueChange<string> LastName { get; set; }

        public OptionalValueChange<string> PhoneNumber { get; set; }

        public OptionalValueChange<string> DefaultUserStartPreference { get; set; }

        public OptionalValueChange<bool> HasApiAccess { get; set; }

        public OptionalValueChange<bool> HasStakeHolderAccess { get; set; }

        public OptionalValueChange<IEnumerable<OrganizationRole>> Roles { get; set; }

        public bool SendMailOnUpdate {get; set; }
        public OptionalValueChange<Guid> DefaultOrganizationUnitUuid { get; set; }

        public bool HasOnlyRoleChanges()
        {
            return (Email == null || Email.HasChange == false) &&
                   (FirstName == null || FirstName.HasChange == false) &&
                   (LastName == null || LastName.HasChange == false) &&
                   (PhoneNumber == null || PhoneNumber.HasChange == false) &&
                   (DefaultUserStartPreference == null || DefaultUserStartPreference.HasChange == false) &&
                   (HasApiAccess == null || HasApiAccess.HasChange == false) &&
                   (HasStakeHolderAccess == null || HasStakeHolderAccess.HasChange == false) &&
                   (DefaultOrganizationUnitUuid == null || DefaultOrganizationUnitUuid.HasChange == false);
        }
    }
}
