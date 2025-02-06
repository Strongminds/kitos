﻿using System.Collections.Generic;
using Presentation.Web.Models.API.V1.Shared;

namespace Presentation.Web.Models.API.V1.GDPR
{
    public class DataProcessingOptionsDTO
    {
        public IEnumerable<OptionWithDescriptionDTO> DataResponsibleOptions { get; set; }
        public IEnumerable<OptionWithDescriptionDTO> ThirdCountryOptions { get; set; }
        public IEnumerable<OptionWithDescriptionDTO> BasisForTransferOptions { get; set; }
        public IEnumerable<DataProcessingBusinessRoleDTO> Roles { get; set; }
        public IEnumerable<OptionWithDescriptionDTO> OversightOptions { get; set; }
    }
}