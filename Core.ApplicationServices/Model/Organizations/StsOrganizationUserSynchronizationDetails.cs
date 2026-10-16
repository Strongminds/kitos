using System;
using Core.DomainServices.Model.StsOrganization;

namespace Core.ApplicationServices.Model.Organizations
{
    public class StsOrganizationUserSynchronizationDetails
    {
        public bool Connected { get; }
        public bool CanCreateConnection { get; }
        public bool CanDeleteConnection { get; }
        public CheckConnectionError? CheckConnectionError { get; }
        public DateTime? ConnectedAt { get; }

        public StsOrganizationUserSynchronizationDetails(bool connected, bool canCreateConnection, bool canDeleteConnection, CheckConnectionError? checkConnectionError, DateTime? connectedAt)
        {
            Connected = connected;
            CanCreateConnection = canCreateConnection;
            CanDeleteConnection = canDeleteConnection;
            CheckConnectionError = checkConnectionError;
            ConnectedAt = connectedAt;
        }
    }
}
