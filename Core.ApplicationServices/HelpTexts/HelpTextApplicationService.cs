using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.HelpTexts;
using Core.DomainModel;
using Core.DomainModel.Events;
using Core.DomainServices;

namespace Core.ApplicationServices.HelpTexts
{
    public class HelpTextApplicationService: IHelpTextApplicationService
    {
        private readonly IGenericRepository<HelpText> _helpTextsRepository;
        private readonly IOrganizationalUserContext _userContext;
        private readonly IDomainEvents _domainEvents;
        private readonly IHelpTextService _helpTextService;

        public HelpTextApplicationService(IGenericRepository<HelpText> helpTextsRepository,
            IOrganizationalUserContext userContext, IDomainEvents domainEvents, IHelpTextService helpTextService)
        {
            _helpTextsRepository = helpTextsRepository;
            _userContext = userContext;
            _domainEvents = domainEvents;
            _helpTextService = helpTextService;
        }

        public Result<IEnumerable<HelpText>, OperationError> GetHelpTexts()
        {
            return Result<IEnumerable<HelpText>, OperationError>.Success(this._helpTextsRepository.AsQueryable().ToList());
        }

        public Result<HelpText, OperationError> CreateHelpText(HelpTextCreateParameters parameters)
        {
            return WithGlobalAdminRights("User is not allowed to create help text")
                .Match(error => error,
                    () => WithAvailableKey(parameters.Key)
                        .Match(error => error,
                            () =>
                            {
                                var newHelpText = new HelpText()
                                {
                                    Description = parameters.Description,
                                    Title = parameters.Title,
                                    Key = parameters.Key
                                };

                                _helpTextsRepository.Insert(newHelpText);
                                _helpTextsRepository.Save();
                                _domainEvents.Raise(new EntityCreatedEvent<HelpText>(newHelpText));

                                return Result<HelpText, OperationError>.Success(newHelpText);
                            }));
        }

        public Maybe<OperationError> DeleteHelpText(string key)
        {
            return WithGlobalAdminRights($"User is not allowed to delete help text with key {key}")
                .Match(error => error,
                    () =>
                    {
                        var helpText = GetByKey(key);
                        if (helpText.Failed) return helpText.Error;
                        var value = helpText.Value;
                        _helpTextsRepository.Delete(value);
                        _domainEvents.Raise(new EntityBeingDeletedEvent<HelpText>(value));
                        return Maybe<OperationError>.None;
                    });
        }

        public Result<HelpText, OperationError> PatchHelpText(HelpTextUpdateParameters parameters)
        {
            return WithGlobalAdminRights($"User is not allowed to patch help text with key { parameters.Key }")
                .Match(error => error,
                    () => GetByKey(parameters.Key).Bind(existing => PerformUpdates(existing, parameters)
                .Bind(updated =>
                {
                    _helpTextsRepository.Update(updated);
                    _helpTextsRepository.Save();
                    _domainEvents.Raise(new EntityUpdatedEvent<HelpText>(updated));
                    return Result<HelpText, OperationError>.Success(updated);
                })));
        }

        private Result<HelpText, OperationError> PerformUpdates(HelpText helpText, HelpTextUpdateParameters parameters)
        {
            return helpText
                .WithOptionalUpdate(parameters.Title, (ht, title) => ht.UpdateTitle(title))
                .Bind(ht => ht.WithOptionalUpdate(parameters.Description,
                    (ht, description) => ht.UpdateDescription(description)));
        }

        private Result<HelpText, OperationError> GetByKey(string key)
        {
            var existing = _helpTextsRepository.AsQueryable().Where(ht => ht.Key == key).FirstOrDefault();
            return existing != null 
                ? existing
                : new OperationError($"Could not find help text with key { key }", OperationFailure.NotFound);
        }

        private Maybe<OperationError> WithAvailableKey(string key)
        {
            return _helpTextService.IsAvailableKey(key)
                ? Maybe<OperationError>.None 
                : Maybe<OperationError>.Some(new OperationError($"A help text with the provided key { key } already exists", OperationFailure.Conflict));

        }

        private Maybe<OperationError> WithGlobalAdminRights(string errorMessage)
        {
            var isGlobalAdmin = _userContext.IsGlobalAdmin();
            return isGlobalAdmin
                ? Maybe<OperationError>.None
                : new OperationError(errorMessage, OperationFailure.Forbidden);
        }
    }
}
