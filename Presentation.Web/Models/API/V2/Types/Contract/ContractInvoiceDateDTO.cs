using System.ComponentModel.DataAnnotations;

namespace Presentation.Web.Models.API.V2.Types.Contract
{
    public class ContractInvoiceDateDTO
    {
        /// <summary>
        /// Calendar period: 1 for Yearly, 1-2 for HalfYearly, 1-4 for Quarterly. Must be unique within the list.
        /// </summary>
        [Range(1, 4)]
        public int Period { get; set; }

        /// <summary>
        /// Invoice month (1-12). Must fall within the selected calendar year, half or quarter.
        /// </summary>
        [Range(1, 12)]
        public int Month { get; set; }

        /// <summary>
        /// Invoice day of month (1-31). Must be valid for the month (29 February is allowed).
        /// </summary>
        [Range(1, 31)]
        public int Day { get; set; }
    }
}
