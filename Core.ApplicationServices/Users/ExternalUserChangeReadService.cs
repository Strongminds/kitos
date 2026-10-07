using System;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.Users;
using Core.DomainModel.Organization;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Users;

namespace Core.ApplicationServices.Users;

public class ExternalUserChangeReadService(
    IOrganizationRepository organizations,
    IOrganizationalUserContext actor,
    IExternalUserChangeReadRepository repository)
{
    public Result<int, OperationError> PendingCount(Guid organizationUuid) =>
        GetOrganizationId(organizationUuid)
            .Select(organizationId => repository.Count(repository.Query(organizationId)
                .Where(x => x.Status == ExternalUserChangeStatus.Pending)));

    public Result<ExternalUserChangeListResult, OperationError> List(
        Guid organizationUuid, ExternalUserChangeListQuery options)
    {
        if (options.Skip < 0 || options.Take is < 1 or > 200 ||
            (options.Status.HasValue && !Enum.IsDefined(options.Status.Value)))
            return new OperationError("Invalid paging or status.", OperationFailure.BadInput);

        var organization = GetOrganizationId(organizationUuid);
        if (organization.Failed) return organization.Error;
        var query = repository.Query(organization.Value);
        var pendingCount = repository.Count(query.Where(x => x.Status == ExternalUserChangeStatus.Pending));
        if (options.Status.HasValue) query = query.Where(x => x.Status == options.Status);
        if (options.ResolvedOnly) query = query.Where(x => x.Status != ExternalUserChangeStatus.Pending);
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            if (options.Search.Length > 200)
                return new OperationError("Search text is too long.", OperationFailure.BadInput);
            query = query.Where(x => x.ExternalMessageId.Contains(options.Search) ||
                (x.User != null && (x.User.Name.Contains(options.Search) ||
                    x.User.LastName.Contains(options.Search) || x.User.Email.Contains(options.Search))));
        }
        var total = repository.Count(query);
        var ordered = options.Sort switch
        {
            "userName" => options.Descending ? query.OrderByDescending(x => x.User!.Name) : query.OrderBy(x => x.User!.Name),
            "status" => options.Descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "receivedAt" => options.Descending ? query.OrderByDescending(x => x.ReceivedAt) : query.OrderBy(x => x.ReceivedAt),
            _ => null
        };
        if (ordered == null) return new OperationError("Unsupported sort field.", OperationFailure.BadInput);
        var records = repository.List(ordered.ThenBy(x => x.Uuid).Skip(options.Skip).Take(options.Take));
        return new ExternalUserChangeListResult(total, pendingCount, records);
    }

    public Result<ExternalUserChange, OperationError> Detail(Guid organizationUuid, Guid changeUuid) =>
        GetOrganizationId(organizationUuid)
            .Bind<ExternalUserChange>(organizationId =>
            {
                var change = repository.List(repository.Query(organizationId).Where(x => x.Uuid == changeUuid)).SingleOrDefault();
                return change == null
                    ? Result<ExternalUserChange, OperationError>.Failure(
                        new OperationError("Change not found.", OperationFailure.NotFound))
                    : change;
            });

    private Result<int, OperationError> GetOrganizationId(Guid organizationUuid)
    {
        var organization = organizations.GetByUuid(organizationUuid);
        if (organization.IsNone) return new OperationError("Organization not found.", OperationFailure.NotFound);
        if (!actor.HasRole(organization.Value.Id, OrganizationRole.LocalAdmin))
            return new OperationError("Local administrator permission required.", OperationFailure.Forbidden);
        return organization.Value.Id;
    }
}
