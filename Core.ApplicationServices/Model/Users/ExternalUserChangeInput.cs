using System;
using Core.DomainModel.Users;

namespace Core.ApplicationServices.Model.Users;

public record ExternalUserChangeInput(string ExternalMessageId, Guid OrganizationUuid,
    Guid ExternalUserUuid, ExternalUserChangeType ChangeType, DateTime? OccurredAt);
