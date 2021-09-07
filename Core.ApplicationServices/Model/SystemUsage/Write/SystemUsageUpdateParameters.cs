﻿using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Shared.Write;


namespace Core.ApplicationServices.Model.SystemUsage.Write
{
    public class SystemUsageUpdateParameters
    {
        public Maybe<UpdatedSystemUsageGeneralProperties> GeneralProperties { get; set; } = Maybe<UpdatedSystemUsageGeneralProperties>.None;
        public Maybe<UpdatedSystemUsageOrganizationalUseParameters> OrganizationalUsage { get; set; } = Maybe<UpdatedSystemUsageOrganizationalUseParameters>.None;
        public Maybe<UpdatedSystemUsageKLEDeviationParameters> KLE { get; set; } = Maybe<UpdatedSystemUsageKLEDeviationParameters>.None;
        public Maybe<IEnumerable<UpdatedExternalReferenceProperties>> ExternalReferences { get; set; } = Maybe<IEnumerable<UpdatedExternalReferenceProperties>>.None;
        public Maybe<UpdatedSystemUsageRoles> Roles { get; set; } = Maybe<UpdatedSystemUsageRoles>.None;
        public Maybe<UpdatedSystemUsageGDPRProperties> GDPR { get; set; } = Maybe<UpdatedSystemUsageGDPRProperties>.None;
        public Maybe<UpdatedSystemUsageArchivingParameters> Archiving { get; set; } = Maybe<UpdatedSystemUsageArchivingParameters>.None;
    }
}
