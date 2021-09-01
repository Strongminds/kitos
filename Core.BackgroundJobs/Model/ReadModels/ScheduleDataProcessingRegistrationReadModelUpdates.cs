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
    public class ScheduleDataProcessingRegistrationReadModelUpdates : IAsyncBackgroundJob
    {
        private readonly IPendingReadModelUpdateRepository _updateRepository;
        private readonly IDataProcessingRegistrationReadModelRepository _readModelRepository;
        private readonly IDataProcessingRegistrationRepository _dataProcessingRegistrationRepository;
        private readonly ITransactionManager _transactionManager;
        public string Id => StandardJobIds.ScheduleDataProcessingRegistrationReadModelUpdates;
        private const int BatchSize = 250;

        public ScheduleDataProcessingRegistrationReadModelUpdates(
            IPendingReadModelUpdateRepository updateRepository,
            IDataProcessingRegistrationReadModelRepository readModelRepository,
            IDataProcessingRegistrationRepository dataProcessingRegistrationRepository,
            ITransactionManager transactionManager)
        {
            _updateRepository = updateRepository;
            _readModelRepository = readModelRepository;
            _dataProcessingRegistrationRepository = dataProcessingRegistrationRepository;
            _transactionManager = transactionManager;
        }

        public Task<Result<string, OperationError>> ExecuteAsync(CancellationToken token = default)
        {
            var updatesExecuted = 0;
            var idsOfDprsAlreadyInQueueForUpdate = _updateRepository
                .GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration, int.MaxValue)
                .Select(x => x.SourceId)
                .ToList();

            var alreadyScheduledIds = new HashSet<int>(idsOfDprsAlreadyInQueueForUpdate);

            updatesExecuted = HandleUserUpdates(token, updatesExecuted, alreadyScheduledIds);
            updatesExecuted = HandleSystemUpdates(token, updatesExecuted, alreadyScheduledIds);
            updatesExecuted = HandleOrganizationUpdates(token, updatesExecuted, alreadyScheduledIds);
            updatesExecuted = HandleBasisForTransferUpdates(token, updatesExecuted, alreadyScheduledIds);
            updatesExecuted = HandleDataResponsibleUpdates(token, updatesExecuted, alreadyScheduledIds);
            updatesExecuted = HandleOversightOptionUpdates(token, updatesExecuted, alreadyScheduledIds);
            updatesExecuted = HandleContractUpdates(token, updatesExecuted, alreadyScheduledIds);

            return Task.FromResult(Result<string, OperationError>.Success($"Completed {updatesExecuted} updates"));
        }
        private int HandleOversightOptionUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var update in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration_OversightOption, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin();
                var ids = _dataProcessingRegistrationRepository.GetByOversightOptionId(update.SourceId).Select(x => x.Id);
                updatesExecuted = PerformUpdate(updatesExecuted, alreadyScheduledIds, ids, update, transaction);
            }

            return updatesExecuted;
        }

        private int HandleDataResponsibleUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var update in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration_DataResponsible, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin();
                var ids = _dataProcessingRegistrationRepository.GetByDataResponsibleId(update.SourceId).Select(x => x.Id);
                updatesExecuted = PerformUpdate(updatesExecuted, alreadyScheduledIds, ids, update, transaction);
            }

            return updatesExecuted;
        }

        private int HandleBasisForTransferUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var update in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration_BasisForTransfer, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin();
                var ids = _dataProcessingRegistrationRepository.GetByBasisForTransferId(update.SourceId).Select(x => x.Id);
                updatesExecuted = PerformUpdate(updatesExecuted, alreadyScheduledIds, ids, update, transaction);
            }

            return updatesExecuted;
        }

        private int HandleOrganizationUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var update in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration_Organization, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin();

                //Org id is not stored in read model so search the source model
                var ids = _dataProcessingRegistrationRepository.GetByDataProcessorId(update.SourceId).Select(x => x.Id);
                updatesExecuted = PerformUpdate(updatesExecuted, alreadyScheduledIds, ids, update, transaction);
            }

            return updatesExecuted;
        }

        private int HandleSystemUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var update in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration_ItSystem, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin();
                //System id is not stored in read model so search the source model
                var ids = _dataProcessingRegistrationRepository.GetBySystemId(update.SourceId).Select(x => x.Id);
                updatesExecuted = PerformUpdate(updatesExecuted, alreadyScheduledIds, ids, update, transaction);
            }

            return updatesExecuted;
        }

        private int HandleContractUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var update in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration_ItContract, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin();
                //Contract id is not stored in read model so search the source model
                var dataProcessingRegistrationIds = _dataProcessingRegistrationRepository.GetByContractId(update.SourceId).Select(x => x.Id);

                updatesExecuted = PerformUpdate(updatesExecuted, alreadyScheduledIds, dataProcessingRegistrationIds, update, transaction);
            }

            return updatesExecuted;
        }

        private int HandleUserUpdates(CancellationToken token, int updatesExecuted, HashSet<int> alreadyScheduledIds)
        {
            foreach (var update in _updateRepository.GetMany(PendingReadModelUpdateSourceCategory.DataProcessingRegistration_User, BatchSize).ToList())
            {
                if (token.IsCancellationRequested)
                    break;

                using var transaction = _transactionManager.Begin();
                var ids = _readModelRepository.GetByUserId(update.SourceId).Select(x => x.SourceEntityId);
                updatesExecuted = PerformUpdate(updatesExecuted, alreadyScheduledIds, ids, update, transaction);
            }

            return updatesExecuted;
        }

        private int PerformUpdate(
            int updatesExecuted,
            HashSet<int> alreadyScheduledIds,
            IQueryable<int> idsOfAffectedDprs,
            PendingReadModelUpdate sourceUpdate,
            IDatabaseTransaction transaction)
        {
            var updates = idsOfAffectedDprs
                .Where(id => alreadyScheduledIds.Contains(id) == false)
                .ToList()
                .Select(id => PendingReadModelUpdate.Create(id, PendingReadModelUpdateSourceCategory.DataProcessingRegistration))
                .ToList();

            updatesExecuted = CompleteUpdate(updatesExecuted, updates, sourceUpdate, transaction);
            updates.ForEach(completedUpdate => alreadyScheduledIds.Add(completedUpdate.SourceId));
            return updatesExecuted;
        }

        private int CompleteUpdate(int updatesExecuted, List<PendingReadModelUpdate> updates, PendingReadModelUpdate userUpdate, IDatabaseTransaction transaction)
        {
            updates.ForEach(update => _updateRepository.Add(update));
            _updateRepository.Delete(userUpdate);
            transaction.Commit();
            updatesExecuted++;
            return updatesExecuted;
        }
    }
}
