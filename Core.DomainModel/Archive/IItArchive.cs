using System;
using System.Collections.Generic;

namespace Core.DomainModel.Archive;

public interface IItArchive : IHasUuid
{
    Guid OrganizationUuid { get; set; }
    Organization.Organization Organization { get; set; }
    ICollection<ArchiveReference> ArchiveReferences { get; set; }
}
