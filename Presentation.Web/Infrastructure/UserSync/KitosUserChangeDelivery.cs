using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Users;
using Core.ApplicationServices.Users;
using Core.DomainModel.Users;
using Microsoft.Extensions.DependencyInjection;
using UserSync.Transport;

namespace Presentation.Web.Infrastructure.UserSync;

public sealed class KitosUserChangeDelivery(IServiceScopeFactory scopes) : IUserChangeDelivery
{
    public async Task<UserChangeDeliveryResult> Deliver(UserDeletionEvent message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = scopes.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ExternalUserChangeIngestionService>();
        var result = service.Ingest(new ExternalUserChangeInput(message.ExternalMessageId,
            message.OrganizationUuid, message.ExternalUserUuid, (ExternalUserChangeType)message.ChangeType,
            message.OccurredAt));
        // Ingest returns success only after SaveChanges (including a concurrent duplicate's commit).
        if (result.Ok) return UserChangeDeliveryResult.Accepted;
        return result.Error.FailureType is OperationFailure.BadInput or OperationFailure.NotFound or OperationFailure.Conflict
            ? UserChangeDeliveryResult.Rejected : UserChangeDeliveryResult.Retry;
    }
}
