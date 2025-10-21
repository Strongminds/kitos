﻿using System.Collections.Generic;
using Core.ApplicationServices.Model.Shared.Write;
using Core.ApplicationServices.Model.SystemUsage.Write;
using Presentation.Web.Models.API.V2.Request.Generic.ExternalReferences;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Presentation.Web.Models.API.V2.Types.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
    public interface IItSystemUsageWriteModelMapper
    {
        SystemUsageUpdateParameters FromPOST(CreateItSystemUsageRequestDTO request);
        SystemUsageUpdateParameters FromPUT(UpdateItSystemUsageRequestDTO request);
        SystemUsageUpdateParameters FromPATCH(UpdateItSystemUsageRequestDTO request);
        SystemRelationParameters MapRelation(SystemRelationWriteRequestDTO relationData);
        IEnumerable<SystemRelationParameters> MapRelations(IEnumerable<SystemRelationWriteRequestDTO> dtos);
        SystemUsageJournalPeriodProperties MapJournalPeriodProperties(JournalPeriodDTO input);
        ExternalReferenceProperties MapExternalReference(ExternalReferenceDataWriteRequestDTO externalReferenceData);
    }
}