
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel;
using NotImplementedException = System.NotImplementedException;

namespace Core.ApplicationServices.HelpTexts
{
    public class HelpTextService: IHelpTextService
    {
        public Result<IEnumerable<HelpText>, OperationError> GetHelpTexts()
        {
            throw new NotImplementedException();
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
