﻿using Presentation.Web.Models.API.V2.Types.Shared;
using System;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Web.Models.API.V2.Types.DataProcessing
{
    public class CreateOversightDateDTO : IOversightDateDTO
    {
        /// <summary>
        /// Date of oversight completion
        /// </summary>
        [Required]
        public DateTime CompletedAt { get; set; }
        /// <summary>
        /// Optional remark related to the oversight
        /// </summary>
        public string Remark { get; set; }

        public SimpleLinkDTO OversightReportLink { get; set; }
    }
}