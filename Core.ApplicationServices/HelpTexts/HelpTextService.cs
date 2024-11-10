
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
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

        public Result<HelpText, OperationError> DeleteHelpText()
        {
            throw new NotImplementedException();
        }

        public Result<HelpText, OperationError> PatchHelpText()
        {
            throw new NotImplementedException();
        }
    }
}
