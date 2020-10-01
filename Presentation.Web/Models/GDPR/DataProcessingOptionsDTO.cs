﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Presentation.Web.Models.Shared;

namespace Presentation.Web.Models.GDPR
{
    public class DataProcessingOptionsDTO
    {
        public IEnumerable<OptionWithDescriptionDTO> dataResponsibleOptions { get; set; }
        public IEnumerable<OptionWithDescriptionDTO> countryOptions { get; set; }
        public IEnumerable<OptionWithDescriptionDTO> basisForTransferOptions { get; set; }
    }
}