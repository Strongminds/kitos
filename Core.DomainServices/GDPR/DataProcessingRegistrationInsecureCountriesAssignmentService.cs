﻿using Core.Abstractions.Types;
using Core.DomainModel.GDPR;
using Core.DomainServices.Options;

namespace Core.DomainServices.GDPR
{
    public class DataProcessingRegistrationInsecureCountriesAssignmentService :
        MultipleOptionTypeInstancesAssignmentServiceBase<DataProcessingRegistration, DataProcessingCountryOption>,
        IDataProcessingRegistrationInsecureCountriesAssignmentService
    {
        public DataProcessingRegistrationInsecureCountriesAssignmentService(IOptionsService<DataProcessingRegistration, DataProcessingCountryOption> optionsService)
        : base(optionsService) { }

        protected override Result<DataProcessingCountryOption, OperationError> PerformAssign(DataProcessingRegistration owner, DataProcessingCountryOption option)
        {
            return owner.AssignInsecureCountrySubjectToDataTransfer(option);
        }

        protected override Result<DataProcessingCountryOption, OperationError> PerformRemove(DataProcessingRegistration owner, DataProcessingCountryOption option)
        {
            return owner.RemoveInsecureCountrySubjectToDataTransfer(option);
        }
    }
}
