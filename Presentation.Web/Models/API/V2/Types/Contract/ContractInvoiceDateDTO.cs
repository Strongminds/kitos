using System;
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
        /// Populated invoice date within the selected calendar year, half or quarter.
        /// Different periods may use different years. Only the date component is stored.
        /// </summary>
        public DateTime Date { get; set; }
    }
}
