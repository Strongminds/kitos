﻿using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.GDPR;
using Core.DomainServices.Options;

namespace Core.ApplicationServices.Model.GDPR
{
    public class DataProcessingRegistrationOptions
    {
        public IReadOnlyList<OptionDescriptor<DataProcessingDataResponsibleOption>> DataResponsibleOptions { get; }
        public IReadOnlyList<OptionDescriptor<DataProcessingCountryOption>> ThirdCountryOptions { get; }
        public IReadOnlyList<OptionDescriptor<DataProcessingBasisForTransferOption>> BasisForTransferOptions { get; }
        public IReadOnlyList<OptionDescriptor<DataProcessingOversightOption>> OversightOptions { get; }

        public DataProcessingRegistrationOptions(
            IEnumerable<OptionDescriptor<DataProcessingDataResponsibleOption>> dataResponsibleOptions,
            IEnumerable<OptionDescriptor<DataProcessingCountryOption>> thirdCountryOptions,
            IEnumerable<OptionDescriptor<DataProcessingBasisForTransferOption>> basisForTransferOptions,
             IEnumerable<OptionDescriptor<DataProcessingOversightOption>> oversightOptions)
        {
            DataResponsibleOptions = dataResponsibleOptions.ToList();
            ThirdCountryOptions = thirdCountryOptions.ToList();
            BasisForTransferOptions = basisForTransferOptions.ToList();
            OversightOptions = oversightOptions.ToList();
        }
    }
}
