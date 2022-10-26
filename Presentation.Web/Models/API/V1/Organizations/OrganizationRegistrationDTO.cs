﻿using System.Collections.Generic;

namespace Presentation.Web.Models.API.V1.Organizations
{
    public class OrganizationRegistrationDTO
    {
        public OrganizationRegistrationDTO()
        {
            OrganizationUnitRights = new List<NamedEntityDTO>();
            ItContractRegistrations = new List<NamedEntityDTO>();
            Payments = new List<PaymentRegistrationDTO>();
            RelevantSystems = new List<NamedEntityWithEnabledStatusDTO>();
            ResponsibleSystems = new List<NamedEntityWithEnabledStatusDTO>();
        }

        public IEnumerable<NamedEntityDTO> OrganizationUnitRights { get; set; }
        public IEnumerable<NamedEntityDTO> ItContractRegistrations { get; set; }
        public IEnumerable<PaymentRegistrationDTO> Payments { get; set; }
        public IEnumerable<NamedEntityWithEnabledStatusDTO> ResponsibleSystems { get; set; }
        public IEnumerable<NamedEntityWithEnabledStatusDTO> RelevantSystems { get; set; }
    }
}