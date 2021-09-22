﻿using Core.DomainModel;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel.KendoConfig;

namespace Core.ApplicationServices
{
    public interface IKendoOrganizationalConfigurationService
    {
        public Result<KendoOrganizationalConfiguration, OperationError> CreateOrUpdate(int organizationId, OverviewType overviewType, IEnumerable<KendoColumnConfiguration> columns);
        public Result<KendoOrganizationalConfiguration, OperationError> Get(int organizationId, OverviewType overviewType);
        public Result<KendoOrganizationalConfiguration, OperationError> Delete(int organizationId, OverviewType overviewType);
    }
}
