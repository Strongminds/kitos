using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.ApplicationServices.Authorization;
using Core.DomainModel.Organization;
using Core.DomainModel.Users;
using Infrastructure.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Web.Controllers.API.V2.Internal.Users;

[Route("api/v2/internal/organization/{organizationUuid:guid}/users/external-changes")]
public class ExternalUserChangesInternalV2Controller(KitosContext db, IOrganizationalUserContext actor,
    Presentation.Web.Services.ExternalUserChangeResolutionService resolution)
    : InternalApiV2Controller
{
    [HttpGet("pending-count")]
    public async Task<IActionResult> PendingCount(Guid organizationUuid, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(x => x.Uuid == organizationUuid, cancellationToken);
        if (organization == null) return NotFound();
        if (!actor.HasRole(organization.Id, OrganizationRole.LocalAdmin)) return Forbid();
        return Ok(new { PendingCount = await db.Set<ExternalUserChange>().CountAsync(
            x => x.OrganizationId == organization.Id && x.Status == ExternalUserChangeStatus.Pending, cancellationToken) });
    }

    public record BulkResolutionRequest(Guid[] ChangeUuids, bool Apply);

    [HttpPost("resolve")]
    public async Task<IActionResult> ResolveBatch(Guid organizationUuid, [FromBody] BulkResolutionRequest request, CancellationToken cancellationToken)
    {
        if (request?.ChangeUuids == null || request.ChangeUuids.Length is < 1 or > 100)
            return BadRequest("Select between 1 and 100 changes.");
        var results = new List<object>();
        foreach (var uuid in request.ChangeUuids.Distinct())
        {
            // Each item has its own transaction; one failed item must not undo successful resolutions.
            db.ChangeTracker.Clear();
            var result = await resolution.Resolve(organizationUuid, uuid, request.Apply, cancellationToken);
            results.Add(result.Ok ? new { Uuid = uuid, Success = true, Error = (string?)null } :
                new { Uuid = uuid, Success = false, Error = (string?)result.Error.Message.GetValueOrFallback(result.Error.FailureType.ToString()) });
        }
        return Ok(results);
    }

    [HttpPost("{changeUuid:guid}/apply")]
    public async Task<IActionResult> Apply(Guid organizationUuid, Guid changeUuid, CancellationToken cancellationToken)
    {
        var result = await resolution.Resolve(organizationUuid, changeUuid, true, cancellationToken);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }

    [HttpPost("{changeUuid:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid organizationUuid, Guid changeUuid, CancellationToken cancellationToken)
    {
        var result = await resolution.Resolve(organizationUuid, changeUuid, false, cancellationToken);
        return result.Match(change => Ok(ExternalUserChangeResponse.From(change)), FromOperationError);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid organizationUuid, ExternalUserChangeStatus? status = null,
        int skip = 0, int take = 50, string? search = null, string sort = "receivedAt", bool descending = true,
        bool resolvedOnly = false, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || skip < 0 || take < 1 || take > 200 || (status.HasValue && !Enum.IsDefined(status.Value)))
            return BadRequest("Invalid paging or status.");
        var organization = await db.Organizations.SingleOrDefaultAsync(x => x.Uuid == organizationUuid, cancellationToken);
        if (organization == null) return NotFound();
        if (!actor.HasRole(organization.Id, OrganizationRole.LocalAdmin)) return Forbid();
        var query = db.Set<ExternalUserChange>().AsNoTracking().Where(x => x.OrganizationId == organization.Id);
        var pendingCount = await query.CountAsync(x => x.Status == ExternalUserChangeStatus.Pending, cancellationToken);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (resolvedOnly) query = query.Where(x => x.Status != ExternalUserChangeStatus.Pending);
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length > 200) return BadRequest("Search text is too long.");
            query = query.Where(x => x.ExternalMessageId.Contains(search) ||
                (x.User != null && (x.User.Name.Contains(search) || x.User.LastName.Contains(search) || x.User.Email.Contains(search))));
        }
        var total = await query.CountAsync(cancellationToken);
        var ordered = sort switch
        {
            "userName" => descending ? query.OrderByDescending(x => x.User!.Name) : query.OrderBy(x => x.User!.Name),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "receivedAt" => descending ? query.OrderByDescending(x => x.ReceivedAt) : query.OrderBy(x => x.ReceivedAt),
            _ => null
        };
        if (ordered == null) return BadRequest("Unsupported sort field.");
        var records = await ordered.ThenBy(x => x.Uuid).Include(x => x.User).Include(x => x.ResolvedByUser)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return Ok(new { Total = total, PendingCount = pendingCount, Items = records.Select(ExternalUserChangeResponse.From) });
    }

    [HttpGet("{changeUuid:guid}")]
    public async Task<IActionResult> Detail(Guid organizationUuid, Guid changeUuid, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(x => x.Uuid == organizationUuid, cancellationToken);
        if (organization == null) return NotFound();
        if (!actor.HasRole(organization.Id, OrganizationRole.LocalAdmin)) return Forbid();
        var record = await db.Set<ExternalUserChange>().AsNoTracking().Include(x => x.User).Include(x => x.ResolvedByUser)
            .SingleOrDefaultAsync(x => x.OrganizationId == organization.Id && x.Uuid == changeUuid, cancellationToken);
        return record == null ? NotFound() : Ok(ExternalUserChangeResponse.From(record));
    }
}
