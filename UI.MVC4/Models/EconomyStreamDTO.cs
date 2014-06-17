﻿using System;
using Core.DomainModel;
using Newtonsoft.Json;
using UI.MVC4.Filters;

namespace UI.MVC4.Models
{
    public class EconomyStreamDTO
    {
        public int Id { get; set; }

        public int? ExternPaymentForId { get; set; }
        public int? InternPaymentForId { get; set; }

        public int? OrganizationUnitId { get; set; }
        public string OrganizationUnitName { get; set; }

        /// <summary>
        /// The field "anskaffelse"
        /// </summary>
        public int Acquisition { get; set; }

        /// <summary>
        /// The field "drift/år"
        /// </summary>
        public int Operation { get; set; }

        public int Other { get; set; }

        /// <summary>
        /// The field "kontering"
        /// </summary>
        public string AccountingEntry { get; set; }

        /// <summary>
        /// Traffic light for audit
        /// </summary>
        public TrafficLight AuditStatus { get; set; }

        /// <summary>
        /// DateTime for audit
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? AuditDate { get; set; }

        public string Note { get; set; }
    }
}