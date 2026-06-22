using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.SystemUsage;
using Core.DomainModel.Archive;
using Core.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.ItSystemUsage;

namespace Core.ApplicationServices.Model.SystemUsage
{
    public class ItSystemArchiveService(
        IItSystemUsageService systemUsageService,
        IAuthorizationContext authorizationContext,
        IGenericRepository<ItSystemArchive> archiveRepository) : IItSystemArchiveService
    {
        public Result<ItSystemArchive, OperationError> Create(Guid systemUsageUuid, ArchiveItSystemUsageParameters parameters)
        {
            var systemUsageResult = systemUsageService.GetItSystemUsageByUuidAndAuthorizeRead(systemUsageUuid);
            if (systemUsageResult.Failed) return systemUsageResult.Error;
            var systemUsage = systemUsageResult.Value;

            if (!authorizationContext.AllowCreate<ItSystemArchive>(systemUsage.OrganizationId))
                return new OperationError("User is not allowed to create it-system archives", OperationFailure.Forbidden);

            var snapshot = CreateSnapshot(systemUsage);
            var archiveReferences = CreateArchiveReferences(parameters);
            var archive = CreateArchive(systemUsage, parameters, snapshot, archiveReferences);

            var createdArchive = archiveRepository.Insert(archive);
            archiveRepository.Save();
            return createdArchive;
        }

        private static ItSystemUsageArchiveSnapshot CreateSnapshot(ItSystemUsage systemUsage)
        {
            var system = systemUsage.ItSystem;
            return new ItSystemUsageArchiveSnapshot
            {
                ItSystemUuid = system.Uuid,
                LegacyName = system.Name,
                LocalId = systemUsage.LocalSystemId,
                LocalName = systemUsage.LocalCallName
            };
        }

        private static List<ArchiveReference> CreateArchiveReferences(ArchiveItSystemUsageParameters parameters)
        {
            return parameters.ArchiveReferences?
                .Select(reference => new ArchiveReference
                {
                    Label = reference.Label,
                    Url = reference.Url
                })
                .ToList() ?? [];
        }

        private static ItSystemArchive CreateArchive(ItSystemUsage systemUsage, ArchiveItSystemUsageParameters parameters, ItSystemUsageArchiveSnapshot snapshot, List<ArchiveReference> archiveReferences)
        {
            return new ItSystemArchive
            {
                OrganizationUuid = systemUsage.Organization.Uuid,
                ArchivingDate = parameters.ArchivingDate,
                ReferenceName = parameters.ReferenceName,
                Note = parameters.Note,
                Snapshot = snapshot,
                ArchiveReferences = archiveReferences
            };
        }
    }
}
