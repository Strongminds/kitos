﻿using System;
using Newtonsoft.Json;
using Presentation.Web.Filters;

namespace Presentation.Web.Models
{
    public class ItProjectPhaseDTO
    {
        public string Name { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? StartDate { get; set; }
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? EndDate { get; set; }
    }
}
