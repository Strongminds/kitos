using System;

namespace Core.ApplicationServices.Model.Users;

public record ExternalUserChangeBatchResult(Guid Uuid, bool Success, string? Error);
