using Core.Abstractions.Types;
using Core.DomainModel.ItSystemUsage.GDPR;
using Presentation.Web.Models.API.V2.Types.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
	public static class GdprCriticalityChoiceMappingExtensions
	{
		private static readonly EnumMap<GdprCriticalityChoice, GdprCriticality> Mapping;

        static GdprCriticalityChoiceMappingExtensions()
        {
            Mapping = new EnumMap<GdprCriticalityChoice, GdprCriticality>(
                (GdprCriticalityChoice.NotCritical, GdprCriticality.NotCritical),
                (GdprCriticalityChoice.Low, GdprCriticality.Low),
                (GdprCriticalityChoice.Medium, GdprCriticality.Medium),
                (GdprCriticalityChoice.High, GdprCriticality.High),
                (GdprCriticalityChoice.VeryHigh, GdprCriticality.VeryHigh)
            );
        }

        public static GdprCriticality ToGdprCriticality(this GdprCriticalityChoice value)
        {
            return Mapping.FromLeftToRight(value);
        }

        public static GdprCriticalityChoice ToGdprCriticalityChoice(this GdprCriticality value)
        {
            return Mapping.FromRightToLeft(value);
        }
    }
}