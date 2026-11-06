using System.Collections.Generic;
using Core.DomainModel.Users;

namespace Core.ApplicationServices.Model.Users;

public record ExternalUserChangeListResult(int Total, int PendingCount, IReadOnlyList<ExternalUserChange> Items);
