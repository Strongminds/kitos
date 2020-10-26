﻿using System;
using Core.DomainModel.GDPR;
using Core.DomainModel.Shared;
using Presentation.Web.Models.References;
using Presentation.Web.Models.Shared;

namespace Presentation.Web.Models.GDPR
{
    public class DataProcessingRegistrationDTO : NamedEntityDTO
    {
        public DataProcessingRegistrationDTO(int id, string name)
            : base(id, name)
        {
        }

        public AssignedRoleDTO[] AssignedRoles { get; set; }

        public ReferenceDTO[] References { get; set; }

        public NamedEntityWithEnabledStatusDTO[] ItSystems { get; set; }

        public ValueWithOptionalRemarkDTO<YearMonthIntervalOption?> OversightInterval { get; set; }

        public YesNoUndecidedOption? HasSubDataProcessors { get; set; }

        public ShallowOrganizationDTO[] DataProcessors { get; set; }
        
        public ShallowOrganizationDTO[] SubDataProcessors { get; set; }

        public ValueOptionWithOptionalDateDTO<YesNoIrrelevantOption?> AgreementConcluded { get; set; }

        public YesNoUndecidedOption? TransferToInsecureThirdCountries { get; set; }

        public NamedEntityWithExpirationStatusDTO[] InsecureThirdCountries { get; set; }

        public NamedEntityWithExpirationStatusDTO BasisForTransfer { get; set; }

        public ValueWithOptionalRemarkDTO<OptionWithDescriptionAndExpirationDTO> DataResponsible { get; set; }

        public ValueWithOptionalRemarkDTO<NamedEntityWithExpirationStatusDTO[]> OversightOptions { get; set; }


        public ValueWithOptionalDateAndRemark<YesNoUndecidedOption?> OversightCompleted { get; set; }

        public NamedEntityDTO[] AssociatedContracts { get; set; }

   
    }
}