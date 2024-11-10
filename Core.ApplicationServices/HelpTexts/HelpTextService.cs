
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.DomainModel;
using Core.DomainServices;
using NotImplementedException = System.NotImplementedException;

namespace Core.ApplicationServices.HelpTexts
{
    public class HelpTextService: IHelpTextService
    {
        private readonly IGenericRepository<HelpText> _helpTextsRepository;
        private readonly IAuthorizationContext _authorizationContext;

        public HelpTextService(IGenericRepository<HelpText> helpTextsRepository)
        {
            this._helpTextsRepository = helpTextsRepository;
        }

        public Result<IEnumerable<HelpText>, OperationError> GetHelpTexts()
        {
            return Result<IEnumerable<HelpText>, OperationError>.Success(this._helpTextsRepository.AsQueryable().ToList());
        }

        public Result<HelpText, OperationError> CreateHelpText()
        {
            throw new NotImplementedException();
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
