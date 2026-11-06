using System;
using Core.ApplicationServices.Model.Users;

namespace Presentation.Web.Models.API.V2.Internal.Response.User;

public record ExternalUserChangeBatchResponse(Guid Uuid, bool Success, string? Error)
{
    public static ExternalUserChangeBatchResponse From(ExternalUserChangeBatchResult result) =>
        new(result.Uuid, result.Success, result.Error);
}
