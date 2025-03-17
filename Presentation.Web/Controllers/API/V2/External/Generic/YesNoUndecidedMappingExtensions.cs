using Core.Abstractions.Types;
using Core.DomainModel.Shared;
using Presentation.Web.Models.API.V2.Types.Shared;

namespace Presentation.Web.Controllers.API.V2.External.Generic
{
    public static class YesNoUndecidedMappingExtensions
    {
        private static readonly EnumMap<YesNoUndecidedChoice, YesNoUndecidedOption> Mapping;

        static YesNoUndecidedMappingExtensions()
        {
            Mapping = new EnumMap<YesNoUndecidedChoice, YesNoUndecidedOption>
            (
                (YesNoUndecidedChoice.Yes, YesNoUndecidedOption.Yes),
                (YesNoUndecidedChoice.No, YesNoUndecidedOption.No),
                (YesNoUndecidedChoice.Undecided, YesNoUndecidedOption.Undecided)
            );
        }

        public static YesNoUndecidedOption ToYesNoPartiallyOption(this YesNoUndecidedChoice value)
        {
            return Mapping.FromLeftToRight(value);
        }

        public static YesNoUndecidedChoice ToYesNoPartiallyChoice(this YesNoUndecidedOption value)
        {
            return Mapping.FromRightToLeft(value);

        }
    }
}