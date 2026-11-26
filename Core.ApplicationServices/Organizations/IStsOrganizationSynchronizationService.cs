using System;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Organizations;
using Core.DomainModel.Organization;

namespace Core.ApplicationServices.Organizations
{
    public interface IStsOrganizationSynchronizationService
    {
        /// <summary>
        /// Gets the synchronization details of the organization
        /// </summary>
        /// <returns></returns>
        Result<StsOrganizationSynchronizationDetails, OperationError> GetSynchronizationDetails(Guid organizationId);
        /// <summary>
        /// Retrieves a view of the organization as it exists in STS Organization
        /// </summary>
        /// <returns></returns>
        Result<ExternalOrganizationUnit, OperationError> GetStsOrganizationalHierarchy(Guid organizationId, Maybe<int> levelsToInclude);

        /// <summary>
        /// Connect the organization to "STS Organisation"
        /// </summary>
        /// <returns></returns>
        Maybe<OperationError> Connect(Guid organizationId, Maybe<int> levelsToInclude, bool subscribeToUpdates);
        /// <summary>
        /// Disconnect the KITOS organization from STS Organisation
        /// </summary>
        /// <returns></returns>
        Maybe<OperationError> Disconnect(Guid organizationId, bool purgeUnusedExternalOrganizationUnits = false);
        /// <summary>
        /// Retrieves a view of the consequences of updating the synchronized hierarchy from that which exists in STS Organization
        /// </summary>
        /// <returns></returns>
        Result<OrganizationTreeUpdateConsequences, OperationError> GetConnectionExternalHierarchyUpdateConsequences(Guid organizationId, Maybe<int> levelsToInclude);
        /// <summary>
        /// Updates the connection to the STS Organization
        /// </summary>
        /// <returns></returns>
        Maybe<OperationError> UpdateConnection(Guid organizationId, Maybe<int> levelsToInclude, bool subscribeToUpdates);
        /// <summary>
        /// Unsubscribes from automatic updates from STS Organization
        /// </summary>
        /// <returns></returns>
        Maybe<OperationError> UnsubscribeFromAutomaticUpdates(Guid organizationId);
        /// Gets the last x change logs for the organization
        /// </summary>
        /// <returns></returns>
        Result<IEnumerable<IExternalConnectionChangelog>, OperationError> GetChangeLogs(Guid organizationUuid, int numberOfChangeLogs);
        /// <summary>
        /// Gets the status of the FK Organisation users connection (independent of the org-unit connection)
        /// </summary>
        Result<StsOrganizationUserSynchronizationDetails, OperationError> GetUserSynchronizationDetails(Guid organizationId);
        /// <summary>
        /// Connects the organization's users to FK Organisation, allowing external user changes to be received
        /// </summary>
        Maybe<OperationError> ConnectUsers(Guid organizationId);
        /// <summary>
        /// Disconnects the organization's users from FK Organisation. Existing external user changes are retained.
        /// </summary>
        Maybe<OperationError> DisconnectUsers(Guid organizationId);
    }
}
