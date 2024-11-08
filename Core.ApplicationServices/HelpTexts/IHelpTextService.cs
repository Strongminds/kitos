

using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel;

namespace Core.ApplicationServices.HelpTexts
{
    public interface IHelpTextService
    {
        Result<IEnumerable<HelpText>, OperationError> GetHelpTexts();

        Result<HelpText, OperationError> CreateHelpText();

        Result<HelpText, OperationError> DeleteHelpText();

        Result<HelpText, OperationError> PatchHelpText();

    }
}
