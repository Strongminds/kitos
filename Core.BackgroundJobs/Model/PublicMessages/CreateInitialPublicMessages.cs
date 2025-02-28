using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Core.ApplicationServices.Messages;
using Core.ApplicationServices.Model.Messages;
using Infrastructure.Services.DataAccess;

namespace Core.BackgroundJobs.Model.PublicMessages;

public class CreateInitialPublicMessages : IAsyncBackgroundJob
{
    private readonly IPublicMessagesService _publicMessageService;
    private readonly ITransactionManager _transactionManager;
    public CreateInitialPublicMessages(IPublicMessagesService publicMessageService, ITransactionManager transactionManager)
    {
        _publicMessageService = publicMessageService;
        _transactionManager = transactionManager;
    }
    public string Id { get; }
    public Task<Result<string, OperationError>> ExecuteAsync(CancellationToken token = default)
    {
        var transaction = _transactionManager.Begin();
        for (int i = 0; i < 6; i++)
        {
            var result = _publicMessageService.Create(new WritePublicMessagesParams());
            if (result.Failed)
            {
                transaction.Rollback();
                return Task.FromResult(
                    Result<string, OperationError>.Failure(new OperationError(OperationFailure.UnknownError)));
            }
        }
        return Task.FromResult(Result<string, OperationError>.Success("Successfully created initial public messages"));
    }
}