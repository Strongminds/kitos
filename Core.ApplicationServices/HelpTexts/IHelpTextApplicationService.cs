

using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.HelpTexts;
using Core.DomainModel;

namespace Core.ApplicationServices.HelpTexts
{
    public interface IHelpTextApplicationService
    {
        Result<HelpText, OperationError> GetHelpText(string key);
        Result<IEnumerable<HelpText>, OperationError> GetHelpTexts();

        Result<HelpText, OperationError> CreateHelpText(HelpTextCreateParameters parameters);

        Maybe<OperationError> DeleteHelpText(string key);

        Result<HelpText, OperationError> PatchHelpText(string key, HelpTextUpdateParameters parameters);

    }
}
