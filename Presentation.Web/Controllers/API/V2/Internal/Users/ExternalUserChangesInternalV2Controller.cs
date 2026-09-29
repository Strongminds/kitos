using System;
using System.Linq;
using System.Threading;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Users;
using Core.DomainModel.Users;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Models.API.V2.Internal.Request.User;
using Presentation.Web.Models.API.V2.Internal.Response.User;

namespace Presentation.Web.Controllers.API.V2.Internal.Users;

[Route("api/v2/internal/organization/{organizationUuid:guid}/users/external-changes")]
public class ExternalUserChangesInternalV2Controller(ExternalUserChangeReadService read,
    ExternalUserChangeResolutionService resolution)
    : InternalApiV2Controller
{
    [HttpGet("pending-count")]
    public IActionResult PendingCount(Guid organizationUuid)
    {
        var result = read.PendingCount(organizationUuid);
        return result.Match(count => Ok(new { PendingCount = count }), FromOperationError);
    }

    [HttpPost("resolve")]
    public IActionResult ResolveBatch(Guid organizationUuid, [FromBody] BulkResolutionRequest request, CancellationToken cancellationToken)
    {
        if (request?.ChangeUuids == null || request.ChangeUuids.Length is < 1 or > 100)
            return BadRequest("Select between 1 and 100 changes.");
        return Ok(resolution.ResolveBatch(organizationUuid, request.ChangeUuids, request.Apply, cancellationToken));
    }

    [HttpPost("{changeUuid:guid}/apply")]
    public IActionResult Apply(Guid organizationUuid, Guid changeUuid)
    {
        var result = resolution.Resolve(organizationUuid, changeUuid, true);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }

    [HttpPost("{changeUuid:guid}/dismiss")]
    public IActionResult Dismiss(Guid organizationUuid, Guid changeUuid)
    {
        var result = resolution.Resolve(organizationUuid, changeUuid, false);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }

    [HttpGet]
    public IActionResult List(Guid organizationUuid, ExternalUserChangeStatus? status = null,
        int skip = 0, int take = 50, string? search = null, string sort = "receivedAt", bool descending = true,
        bool resolvedOnly = false)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid paging or status.");
        var result = read.List(organizationUuid,
            new ExternalUserChangeListQuery(status, skip, take, search, sort, descending, resolvedOnly));
        return result.Match(page => Ok(new
        {
            page.Total, page.PendingCount, Items = page.Items.Select(ExternalUserChangeResponse.From)
        }), FromOperationError);
    }

    [HttpGet("{changeUuid:guid}")]
    public IActionResult Detail(Guid organizationUuid, Guid changeUuid)
    {
        var result = read.Detail(organizationUuid, changeUuid);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }
}
