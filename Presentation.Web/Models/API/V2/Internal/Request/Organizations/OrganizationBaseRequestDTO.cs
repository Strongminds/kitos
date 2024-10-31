﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Presentation.Web.Models.API.V2.Internal.Request.Organizations
{
    public class OrganizationBaseRequestDTO
    {
        [Required]
        public string Name { get; set; }
        [Required]
        [Range(1, 4)]
        public int TypeId { get; set; }
        public string Cvr {  get; set; }
        public string ForeignCvr { get; set; }
    }
}