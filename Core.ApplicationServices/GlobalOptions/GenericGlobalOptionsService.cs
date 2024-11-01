﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.GlobalOptions;
using Core.DomainModel;
using Core.DomainModel.Events;
using Core.DomainServices;
using Core.DomainServices.Authorization;
using Core.DomainServices.Extensions;

namespace Core.ApplicationServices.GlobalOptions;

public class GenericGlobalOptionsService<TOptionType, TReferenceType> : IGenericGlobalOptionsService<TOptionType, TReferenceType> 
    where TOptionType : OptionEntity<TReferenceType>, new()
{
    private readonly IGenericRepository<TOptionType> _globalOptionsRepository;
    private readonly IOrganizationalUserContext _activeUserContext;
    private readonly IDomainEvents _domainEvents;


    public GenericGlobalOptionsService(IGenericRepository<TOptionType> globalOptionsRepository, IOrganizationalUserContext activeUserContext, IDomainEvents domainEvents)
    {
        _globalOptionsRepository = globalOptionsRepository;
        _activeUserContext = activeUserContext;
        _domainEvents = domainEvents;
    }
    public Result<IEnumerable<TOptionType>, OperationError> GetGlobalOptions()
    {
        return WithGlobalAdminRights("User is not allowed to read global options")
            .Match(error => error,
                () =>
                {
                    var globalOptions = _globalOptionsRepository.AsQueryable().ToList();
                    return Result<IEnumerable<TOptionType>, OperationError>.Success(globalOptions);
                });
    }

    public Result<TOptionType, OperationError> CreateGlobalOption(GlobalOptionCreateParameters createParameters)
    {
        return WithGlobalAdminRights("User is not allowed to create global options")
            .Match(error => error,
                () =>
                {
                    var globalOption = new TOptionType()
                    {
                        Name = createParameters.Name,
                        Description = createParameters.Description,
                        IsObligatory = createParameters.IsObligatory,
                        IsEnabled = false,
                    };
                    _globalOptionsRepository.Insert(globalOption);
                    _globalOptionsRepository.Save();
                    _domainEvents.Raise(new EntityCreatedEvent<TOptionType>(globalOption));

                    return Result<TOptionType, OperationError>.Success(globalOption);
                });
    }

    public Result<TOptionType, OperationError> PatchGlobalOption(Guid optionUuid, GlobalOptionUpdateParameters updateParameters)
    {
        return WithGlobalAdminRights($"User is not allowed to patch global option with uuid {optionUuid}")
            .Match(error => error,
                () =>
                {
                    var existingOption = _globalOptionsRepository.AsQueryable().ByUuid(optionUuid);
                    if (existingOption == null)
                        return new OperationError($"Unable to find global option with uuid {optionUuid}",
                            OperationFailure.NotFound);

                    return PerformGlobalOptionUpdates(existingOption, updateParameters)
                        .Bind(updatedOption =>
                        {
                            _globalOptionsRepository.Update(updatedOption);
                            _globalOptionsRepository.Save();
                            _domainEvents.Raise(new EntityUpdatedEvent<TOptionType>(updatedOption));

                            return Result<TOptionType, OperationError>.Success(updatedOption);
                        });
                });
    }

    private Result<TOptionType, OperationError> PerformGlobalOptionUpdates(TOptionType option, GlobalOptionUpdateParameters parameters)
    {
        return option
            .WithOptionalUpdate(parameters.Description, (opt, description) => opt.UpdateDescription(description))
            .Bind(opt => opt.WithOptionalUpdate(parameters.Name, (opt, name) => opt.UpdateName(name)))
            .Bind(opt => opt.WithOptionalUpdate(parameters.IsObligatory, (opt, isObligatory) => opt.UpdateIsObligatory(isObligatory)))
            .Bind(opt => opt.WithOptionalUpdate(parameters.IsEnabled, (opt, isEnabled) => opt.UpdateIsEnabled(isEnabled)));
    }


    private Maybe<OperationError> WithGlobalAdminRights(string errorMessage)
    {
        var isGlobalAdmin = _activeUserContext.IsGlobalAdmin();
        return isGlobalAdmin 
            ? Maybe<OperationError>.None 
            : new OperationError(errorMessage, OperationFailure.Forbidden);
    }
}