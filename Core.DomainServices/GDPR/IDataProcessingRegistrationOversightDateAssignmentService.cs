﻿using System;
using Core.Abstractions.Types;
using Core.DomainModel.GDPR;

namespace Core.DomainServices.GDPR
{
    public interface IDataProcessingRegistrationOversightDateAssignmentService
    {
        public Result<DataProcessingRegistrationOversightDate, OperationError> Assign(DataProcessingRegistration registration, DateTime oversightDate, string oversightRemark, string oversightReportLink, string oversightReportLinkName);
        public Result<DataProcessingRegistrationOversightDate, OperationError> Remove(DataProcessingRegistration registration, int oversightId);
        public Result<DataProcessingRegistrationOversightDate, OperationError> Modify(DataProcessingRegistration registration, int oversightId, DateTime oversightDate, string oversightRemark);
    }
}
