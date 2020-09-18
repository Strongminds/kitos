﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.DomainModel.BackgroundJobs;
using Core.DomainModel.Result;
using Core.DomainServices.Repositories.BackgroundJobs;
using Core.DomainServices.Repositories.GDPR;
using Infrastructure.Services.DataAccess;

namespace Core.BackgroundJobs.Model.ReadModels
{
    /// <summary>
    /// Based on updated dependencies, this job schedules new job
    /// </summary>
    public class ScheduleDataProcessingAgreementReadModelUpdates : IAsyncBackgroundJob
    {
        private readonly IPendingReadModelUpdateRepository _updateRepository;
        private readonly IDataProcessingAgreementReadModelRepository _readModelRepository;
        private readonly IDataProcessingAgreementRepository _dataProcessingAgreementRepository;
        private readonly ITransactionManager _transactionManager;
        public string Id => StandardJobIds.ScheduleDataProcessingAgreementReadModelUpdates;
        private const int BatchSize = 250;

        public ScheduleDataProcessingAgreementReadModelUpdates(
            IPendingReadModelUpdateRepository updateRepository,
            IDataProcessingAgreementReadModelRepository readModelRepository,
            IDataProcessingAgreementRepository dataProcessingAgreementRepository,
            ITransactionManager transactionManager)
        {
            _updateRepository = updateRepository;
            _readModelRepository = readModelRepository;
            _dataProcessingAgreementRepository = dataProcessingAgreementRepository;
            _transactionManager = transactionManager;
        }

        public Task<Result<string, OperationError>> ExecuteAsync(CancellationToken token = default)
        {
            var updatesExecuted = 0;
            var alreadyScheduledIds = new HashSet<int>();

            updatesExecuted = HandleUserUpdates(token, updatesExecuted, alreadyScheduledIds);
            updatesExecuted = HandleSystemUpdates(token, updatesExecuted, alreadyScheduledIds);

            return Task.FromResult(Result<string, OperationError>.Success($"Completed {updatesExecuted} updates"));
        }

        private int HandleSystemUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var userUpdate in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingAgreement_ItSystem, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin(IsolationLevel.ReadCommitted);
                var updates = _dataProcessingAgreementRepository.GetBySystemId(userUpdate.SourceId) //System id is not stored in read model so search the source model
                    .Where(x => alreadyScheduledIds.Contains(x.Id) == false)
                    .ToList()
                    .Select(dpa => PendingReadModelUpdate.Create(dpa.Id, PendingReadModelUpdateSourceCategory.DataProcessingAgreement))
                    .ToList();

                updatesExecuted = CompleteUpdate(updatesExecuted, updates, userUpdate, transaction);
            }

            return updatesExecuted;
        }

        private int HandleUserUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var userUpdate in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingAgreement_User, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin(IsolationLevel.ReadCommitted);
                var updates = _readModelRepository.GetByUserId(userUpdate.SourceId)
                    .Where(x => alreadyScheduledIds.Contains(x.SourceEntityId) == false)
                    .ToList()
                    .Select(dpa => PendingReadModelUpdate.Create(dpa.SourceEntityId, PendingReadModelUpdateSourceCategory.DataProcessingAgreement))
                    .ToList();

                updatesExecuted = CompleteUpdate(updatesExecuted, updates, userUpdate, transaction);
            }

            return updatesExecuted;
        }

        private int CompleteUpdate(int updatesExecuted, List<PendingReadModelUpdate> updates, PendingReadModelUpdate userUpdate,
            IDatabaseTransaction transaction)
        {
            updates.ForEach(update => _updateRepository.AddIfNotPresent(update));
            _updateRepository.Delete(userUpdate);
            transaction.Commit();
            updatesExecuted++;
            return updatesExecuted;
        }
    }
}
