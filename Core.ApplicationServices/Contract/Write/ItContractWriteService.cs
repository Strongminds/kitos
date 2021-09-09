﻿using System;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.Contracts.Write;
using Core.ApplicationServices.Model.Shared;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel;
using Core.DomainModel.Events;
using Core.DomainModel.ItContract;
using Core.DomainModel.Organization;
using Core.DomainServices.Generic;
using Infrastructure.Services.DataAccess;


namespace Core.ApplicationServices.Contract.Write
{
    public class ItContractWriteService : IItContractWriteService
    {
        private readonly IItContractService _contractService;
        private readonly IEntityIdentityResolver _entityIdentityResolver;
        private readonly IOptionResolver _optionResolver;
        private readonly ITransactionManager _transactionManager;
        private readonly IDomainEvents _domainEvents;
        private readonly IDatabaseControl _databaseControl;

        public ItContractWriteService(
            IItContractService contractService,
            IEntityIdentityResolver entityIdentityResolver,
            IOptionResolver optionResolver,
            ITransactionManager transactionManager,
            IDomainEvents domainEvents,
            IDatabaseControl databaseControl)
        {
            _contractService = contractService;
            _entityIdentityResolver = entityIdentityResolver;
            _optionResolver = optionResolver;
            _transactionManager = transactionManager;
            _domainEvents = domainEvents;
            _databaseControl = databaseControl;
        }

        public Result<ItContract, OperationError> Create(Guid organizationUuid, ItContractModificationParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (parameters.Name.IsUnchanged)
                return new OperationError("Name must be provided", OperationFailure.BadInput);

            using var transaction = _transactionManager.Begin();

            var orgId = _entityIdentityResolver.ResolveDbId<Organization>(organizationUuid);
            if (orgId.IsNone)
                return new OperationError("Organization id not valid", OperationFailure.BadInput);

            var nameNewValue = parameters.Name.NewValue;

            parameters.Name = OptionalValueChange<string>.None; //Do not update after creation

            var result = _contractService
                .Create(orgId.Value, nameNewValue)
                .Bind(contract => ApplyUpdates(contract, parameters));

            if (result.Ok)
            {
                _databaseControl.SaveChanges();
                transaction.Commit();
            }

            return result;
        }

        public Result<ItContract, OperationError> Update(Guid itContractUuid, ItContractModificationParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using var transaction = _transactionManager.Begin();

            var updateResult = _contractService
                .GetContract(itContractUuid)
                .Bind(contract => ApplyUpdates(contract, parameters));

            if (updateResult.Ok)
            {
                _domainEvents.Raise(new EntityUpdatedEvent<ItContract>(updateResult.Value));
                _databaseControl.SaveChanges();
                transaction.Commit();
            }

            return updateResult;
        }

        private Result<ItContract, OperationError> ApplyUpdates(ItContract contract, ItContractModificationParameters parameters)
        {
            return contract.WithOptionalUpdate(parameters.Name, UpdateName)
                .Bind(updateContract => updateContract.WithOptionalUpdate(parameters.ParentContractUuid, UpdateParentContract))
                .Bind(updateContract => updateContract.WithOptionalUpdate(parameters.Procurement, UpdateProcurement));
        }

        private Result<ItContract, OperationError> UpdateProcurement(ItContract contract, ItContractProcurementModificationParameters procurementParameters)
        {
            return contract
                .WithOptionalUpdate(procurementParameters.ProcurementStrategyUuid, UpdateProcurementStrategy)
                .Bind(itContract => itContract.WithOptionalUpdate(procurementParameters.PurchaseTypeUuid, UpdatePurchaseType))
                .Bind(itContract => UpdateProcurementPlan(itContract, procurementParameters));
        }

        private static Result<ItContract, OperationError> UpdateProcurementPlan(ItContract contract, ItContractProcurementModificationParameters procurementParameters)
        {
            if (procurementParameters.HalfOfYear.IsUnchanged && procurementParameters.Year.IsUnchanged)
                return contract;

            var newHalfOfYear = procurementParameters.HalfOfYear.NewValue;
            var newYear = procurementParameters.Year.NewValue;

            var updateResult = contract.UpdateProcurementPlan(newHalfOfYear, newYear);
            if (updateResult.HasValue)
                return new OperationError($"Failed to update procurement plan with error message: {updateResult.Value.Message.GetValueOrEmptyString()}", updateResult.Value.FailureType);

            return contract;
        }

        private Maybe<OperationError> UpdatePurchaseType(ItContract contract, Guid? purchaseTypeUuid)
        {
            return UpdateIndependentOptionTypeAssignment(
                contract,
                purchaseTypeUuid,
                c => c.ResetPurchaseForm(),
                c => c.PurchaseForm,
                (c, newValue) => c.PurchaseForm = newValue
            );
        }

        private Maybe<OperationError> UpdateProcurementStrategy(ItContract contract, Guid? procurementStrategyUuid)
        {
            return UpdateIndependentOptionTypeAssignment(
                contract,
                procurementStrategyUuid,
                c => c.ResetProcurementStrategy(),
                c => c.ProcurementStrategy,
                (c, newValue) => c.ProcurementStrategy = newValue
            );
        }

        private Maybe<OperationError> UpdateParentContract(ItContract contract, Guid? newParentUuid)
        {
            if (!newParentUuid.HasValue)
            {
                contract.ClearParent();
                return Maybe<OperationError>.None;
            }

            var getResult = _contractService.GetContract(newParentUuid.Value);

            if (getResult.Failed)
                return new OperationError($"Failed to get contract with Uuid: {newParentUuid.Value} with error message: {getResult.Error.Message.GetValueOrEmptyString()}", getResult.Error.FailureType);

            var assignResult = contract.SetParent(getResult.Value);
            if(assignResult.Failed)
                return new OperationError($"Failed to set parent with Uuid: {newParentUuid.Value} on contract with Uuid: {contract.Uuid} with error message: {assignResult.Error.Message.GetValueOrEmptyString()}", assignResult.Error.FailureType);

            return Maybe<OperationError>.None;
        }

        private Maybe<OperationError> UpdateName(ItContract contract, string newName)
        {
            var error = _contractService.ValidateNewName(contract.Id, newName);

            if (error.HasValue)
                return error;

            contract.Name = newName;
            return Maybe<OperationError>.None;
        }

        public Maybe<OperationError> Delete(Guid itContractUuid)
        {
            var dbId = _entityIdentityResolver.ResolveDbId<ItContract>(itContractUuid);

            if (dbId.IsNone)
                return new OperationError("Invalid contract uuid", OperationFailure.NotFound);

            return _contractService
                .Delete(dbId.Value)
                .Match(_ => Maybe<OperationError>.None, failure => new OperationError("Failed deleting contract", failure));
        }

        private Maybe<OperationError> UpdateIndependentOptionTypeAssignment<TOption>(
            ItContract contract,
            Guid? optionTypeUuid,
            Action<ItContract> onReset,
            Func<ItContract, TOption> getCurrentValue,
            Action<ItContract, TOption> updateValue) where TOption : OptionEntity<ItContract>
        {
            if (optionTypeUuid == null)
            {
                onReset(contract);
            }
            else
            {
                var optionType = _optionResolver.GetOptionType<ItContract, TOption>(contract.Organization.Uuid, optionTypeUuid.Value);
                if (optionType.Failed)
                {
                    return new OperationError($"Failure while resolving {typeof(TOption).Namespace} option:{optionType.Error.Message.GetValueOrEmptyString()}", optionType.Error.FailureType);
                }

                var option = optionType.Value;
                var currentValue = getCurrentValue(contract);
                if (option.available == false && (currentValue == null || currentValue.Uuid != optionTypeUuid.Value))
                {
                    return new OperationError($"The changed {typeof(TOption).Namespace} points to an option which is not available in the organization", OperationFailure.BadInput);
                }

                updateValue(contract, option.option);
            }

            return Maybe<OperationError>.None;
        }
    }
}
