
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.HelpTexts;
using Core.DomainModel;
using Core.DomainServices;
using NotImplementedException = System.NotImplementedException;

namespace Core.ApplicationServices.HelpTexts
{
    public class HelpTextService: IHelpTextService
    {
        private readonly IGenericRepository<HelpText> _helpTextsRepository;
        private readonly IOrganizationalUserContext _userContext;

        public HelpTextService(IGenericRepository<HelpText> helpTextsRepository,
            IOrganizationalUserContext userContext)
        {
            _helpTextsRepository = helpTextsRepository;
            _userContext = userContext;
        }

        public Result<IEnumerable<HelpText>, OperationError> GetHelpTexts()
        {
            return Result<IEnumerable<HelpText>, OperationError>.Success(this._helpTextsRepository.AsQueryable().ToList());
        }

        public Result<HelpText, OperationError> CreateHelpText(HelpTextCreateParameters parameters)
        {
            if (!_userContext.IsGlobalAdmin())
                return new OperationError("User is not allowed to create help text", OperationFailure.Forbidden);
            
            var newHelpText = new HelpText()
            {
                Description = parameters.Description,
                Title = parameters.Title,
                Key = parameters.Key
            };

            _helpTextsRepository.Insert(newHelpText);
            _helpTextsRepository.Save();

            return newHelpText;
        }

        public Maybe<OperationError> DeleteHelpText(string key)
        {
            throw new NotImplementedException();
        }

        public Result<HelpText, OperationError> PatchHelpText(HelpTextUpdateParameters parameters)
        {
            if (!_userContext.IsGlobalAdmin())
                return new OperationError($"User is not allowed to patch help text with key { parameters.Key }", OperationFailure.Forbidden);

            return GetByKey(parameters.Key).Bind(existing => PerformUpdates(existing, parameters)
                .Bind(updated =>
                {
                    _helpTextsRepository.Update(updated);
                    _helpTextsRepository.Save();
                    return Result<HelpText, OperationError>.Success(updated);
                }));
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
    }
}
