﻿using Core.DomainModel;
using Core.DomainModel.Result;
using System.Collections.Generic;

namespace Core.ApplicationServices
{
    public interface IKendoOrganizationalConfigurationService
    {
        public Result<KendoOrganizationalConfiguration, OperationError> CreateOrUpdate(int organizationId, OverviewType overviewType, IEnumerable<KendoColumnConfiguration> columns);
        public Result<KendoOrganizationalConfiguration, OperationError> Get(int organizationId, OverviewType overviewType);
        public Result<string, OperationError> GetVersion(int organizationId, OverviewType overviewType);
        public Result<KendoOrganizationalConfiguration, OperationError> Delete(int organizationId, OverviewType overviewType);
    }
}
