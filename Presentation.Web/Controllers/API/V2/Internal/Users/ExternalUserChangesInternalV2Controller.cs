using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Users;
using Core.DomainModel.Users;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.User;
using Presentation.Web.Models.API.V2.Internal.Response.User;

namespace Presentation.Web.Controllers.API.V2.Internal.Users;

[Route("api/v2/internal/organization/{organizationUuid:guid}/users/external-changes")]
public class ExternalUserChangesInternalV2Controller(ExternalUserChangeReadService read,
    ExternalUserChangeResolutionService resolution)
    : InternalApiV2Controller
{
    /// <summary>Gets the number of unresolved external user changes for an organization.</summary>
    [HttpGet("pending-count")]
    [ApiResponse(typeof(ExternalUserChangePendingCountResponse), HttpStatusCode.OK)]
    [ApiResponse(HttpStatusCode.NotFound)]
    [ApiResponse(HttpStatusCode.Forbidden)]
    [ApiResponse(HttpStatusCode.Unauthorized)]
    public IActionResult PendingCount([NonEmptyGuid] Guid organizationUuid)
    {
        var result = read.PendingCount(organizationUuid);
        return result.Match(count => Ok(new ExternalUserChangePendingCountResponse(count)), FromOperationError);
    }

    /// <summary>Applies or dismisses a batch of external user changes, returning a result for each item.</summary>
    [HttpPost("resolve")]
    [ApiResponse(typeof(IEnumerable<ExternalUserChangeBatchResponse>), HttpStatusCode.OK)]
    [ApiResponse(HttpStatusCode.BadRequest)]
    [ApiResponse(HttpStatusCode.Unauthorized)]
    public IActionResult ResolveBatch([NonEmptyGuid] Guid organizationUuid, [FromBody] BulkResolutionRequest request, CancellationToken cancellationToken)
    {
        if (request?.ChangeUuids == null || request.ChangeUuids.Length is < 1 or > 100)
            return BadRequest("Select between 1 and 100 changes.");
        return Ok(resolution.ResolveBatch(organizationUuid, request.ChangeUuids, request.Apply, cancellationToken)
            .Select(ExternalUserChangeBatchResponse.From));
    }

    /// <summary>Applies a pending external deletion change.</summary>
    [HttpPost("{changeUuid:guid}/apply")]
    [ApiResponse(typeof(ExternalUserChangeResponse), HttpStatusCode.OK)]
    [ApiResponse(HttpStatusCode.BadRequest)]
    [ApiResponse(HttpStatusCode.NotFound)]
    [ApiResponse(HttpStatusCode.Conflict)]
    [ApiResponse(HttpStatusCode.Forbidden)]
    [ApiResponse(HttpStatusCode.Unauthorized)]
    public IActionResult Apply([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid changeUuid)
    {
        var result = resolution.Resolve(organizationUuid, changeUuid, true);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }

    /// <summary>Dismisses a pending external user change without applying it.</summary>
    [HttpPost("{changeUuid:guid}/dismiss")]
    [ApiResponse(typeof(ExternalUserChangeResponse), HttpStatusCode.OK)]
    [ApiResponse(HttpStatusCode.NotFound)]
    [ApiResponse(HttpStatusCode.Conflict)]
    [ApiResponse(HttpStatusCode.Forbidden)]
    [ApiResponse(HttpStatusCode.Unauthorized)]
    public IActionResult Dismiss([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid changeUuid)
    {
        var result = resolution.Resolve(organizationUuid, changeUuid, false);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }

    /// <summary>Lists external user changes for an organization.</summary>
    [HttpGet]
    [ApiResponse(typeof(ExternalUserChangeListResponse), HttpStatusCode.OK)]
    [ApiResponse(HttpStatusCode.BadRequest)]
    [ApiResponse(HttpStatusCode.NotFound)]
    [ApiResponse(HttpStatusCode.Forbidden)]
    [ApiResponse(HttpStatusCode.Unauthorized)]
    public IActionResult List([NonEmptyGuid] Guid organizationUuid, ExternalUserChangeStatus? status = null,
        int skip = 0, int take = 50, string? search = null, string sort = "receivedAt", bool descending = true,
        bool resolvedOnly = false)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid paging or status.");
        var result = read.List(organizationUuid,
            new ExternalUserChangeListQuery(status, skip, take, search, sort, descending, resolvedOnly));
        return result.Match(page => Ok(new ExternalUserChangeListResponse(page.Total, page.PendingCount,
            page.Items.Select(ExternalUserChangeResponse.From).ToList())), FromOperationError);
    }

    /// <summary>Gets the details of an external user change.</summary>
    [HttpGet("{changeUuid:guid}")]
    [ApiResponse(typeof(ExternalUserChangeResponse), HttpStatusCode.OK)]
    [ApiResponse(HttpStatusCode.NotFound)]
    [ApiResponse(HttpStatusCode.Forbidden)]
    [ApiResponse(HttpStatusCode.Unauthorized)]
    public IActionResult Detail([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid changeUuid)
    {
        var result = read.Detail(organizationUuid, changeUuid);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }
}
