using Core.DomainModel.Users;

namespace Core.ApplicationServices.Model.Users;

public record ExternalUserChangeListQuery(ExternalUserChangeStatus? Status, int Skip, int Take,
    string? Search, string Sort, bool Descending, bool ResolvedOnly);
