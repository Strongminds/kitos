using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Shared;

namespace Core.ApplicationServices.Model.LocalOptions
{
    public class LocalOptionUpdateParameters
    {
        public OptionalValueChange<Maybe<string>> Description { get; set; }
        public OptionalValueChange<Maybe<bool>> IsExternallyUsed { get; set; }
        public OptionalValueChange<Maybe<string>> ExternallyUsedDescription { get; set; }
    }
}
