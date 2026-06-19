using Core.Abstractions.Types;
using Core.DomainModel.Archive;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Generic;
using Core.DomainServices.Repositories.SystemUsage;
using System;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public class ItSystemUsageArchiveService(
        IItSystemUsageRepository itSystemUsageRepository,
        IEntityIdentityResolver entityIdentityResolver,
        IGenericRepository<ItSystemArchive> archiveRepository,
        IGenericRepository<Organization> organizationRepository) : IItSystemUsageArchiveService
    {
        public Result<ItSystemArchive, OperationError> Create(Guid systemUsageUuid, CreateItSystemArchiveParameters parameters)
        {
            var systemUsageId = entityIdentityResolver.ResolveDbId<ItSystemUsage>(systemUsageUuid);
            if (systemUsageId.IsNone) return new OperationError($"Id for system usage with uuid was not found: {systemUsageUuid}", OperationFailure.NotFound);
            
            var systemUsage = itSystemUsageRepository.GetSystemUsage(systemUsageId.Value);
            if (systemUsage == null) return new OperationError($"System usage with uuid was not found: {systemUsageUuid}", OperationFailure.NotFound);

            var organization = organizationRepository.GetByKey(systemUsage.OrganizationId);
            if (organization == null) return new OperationError($"Organization for system usage not found", OperationFailure.NotFound);

            var archive = new ItSystemArchive
            {
                OrganizationUuid = organization.Uuid,
                ArchivingDate = parameters.ArchivingDate,
                ReferenceName = parameters.ReferenceName,
                Note = parameters.Note,
                ItSystemUsageSnapshotUuid = Guid.NewGuid(),
            };

            var createdArchive = archiveRepository.Insert(archive);
            return createdArchive;
        }
    }
}
