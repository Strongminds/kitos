using System.Collections.Generic;

namespace Presentation.Web.Models.API.V2.Internal.Response.User;

public record ExternalUserChangeListResponse(int Total, int PendingCount,
    IReadOnlyList<ExternalUserChangeResponse> Items);
