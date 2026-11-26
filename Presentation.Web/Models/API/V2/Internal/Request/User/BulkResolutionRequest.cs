using System;

namespace Presentation.Web.Models.API.V2.Internal.Request.User;

public record BulkResolutionRequest(Guid[] ChangeUuids, bool Apply);
