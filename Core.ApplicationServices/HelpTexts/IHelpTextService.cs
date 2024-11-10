

using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.HelpTexts;
using Core.DomainModel;

namespace Core.ApplicationServices.HelpTexts
{
    public interface IHelpTextService
    {
        Result<IEnumerable<HelpText>, OperationError> GetHelpTexts();

        Result<HelpText, OperationError> CreateHelpText(HelpTextCreateParameters parameters);

        Result<HelpText, OperationError> DeleteHelpText();

        Result<HelpText, OperationError> PatchHelpText();

    }
}
