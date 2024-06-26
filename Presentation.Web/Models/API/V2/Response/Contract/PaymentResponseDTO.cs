﻿using System;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Models.API.V2.Types.Contract;

namespace Presentation.Web.Models.API.V2.Response.Contract
{
    public class PaymentResponseDTO
    {
        /// <summary>
        /// Id of the payment
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Optionally defined the organization unit responsible for the payment
        /// </summary>
        public OrganizationUnitResponseDTO OrganizationUnit { get; set; }
        /// <summary>
        /// Part of payment which covers acquisition
        /// </summary>
        public int Acquisition { get; set; }
        /// <summary>
        /// Part of payment which covers operations
        /// </summary>
        public int Operation { get; set; }
        /// <summary>
        /// Part of payment which is not classified as either operations or acquisition
        /// </summary>
        public int Other { get; set; }
        public string AccountingEntry { get; set; }
        /// <summary>
        /// The result of the specific payment audit
        /// </summary>
        public PaymentAuditStatus AuditStatus { get; set; }
        /// <summary>
        /// Defines the date at which the payment was audited
        /// </summary>
        public DateTime? AuditDate { get; set; }
        public string Note { get; set; }
    }
}