﻿using Core.Abstractions.Types;
using Core.ApplicationServices.SystemUsage.GDPR;
using Core.DomainModel.ItSystemUsage.GDPR;
using Core.DomainModel.Organization;
using Core.DomainServices.Generic;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Response;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping;

namespace Presentation.Web.Controllers.API.V2.Internal
{
     
    [RoutePrefix("api/v2/internal/gdpr-report")]
    public class GdprExportReportInternalV2Controller : InternalApiV2Controller
    {
        private readonly IGDPRExportService _gdprExportService;
        private readonly IEntityIdentityResolver _entityIdentityResolver;

        public GdprExportReportInternalV2Controller(IGDPRExportService gdprExportService, IEntityIdentityResolver entityIdentityResolver)
        {
            _gdprExportService = gdprExportService;
            _entityIdentityResolver = entityIdentityResolver;
        }

        [HttpGet]
        [Route("{organizationUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<GdprReportResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetGdprReport([FromUri][NonEmptyGuid] Guid organizationUuid)
        {
            return _entityIdentityResolver.ResolveDbId<Organization>(organizationUuid)
                    .Match(_gdprExportService.GetGDPRData, () => new OperationError($"Cannot find organization with uuid {organizationUuid}", OperationFailure.NotFound))
                    .Select(MapGdprDataToDTO)
                    .Match(Ok, FromOperationError);
        }

        private IEnumerable<GdprReportResponseDTO> MapGdprDataToDTO(IEnumerable<GDPRExportReport> gdprData)
        {
            return gdprData.Select(MapGdprReportToDTO).ToList();
        }

        private GdprReportResponseDTO MapGdprReportToDTO(GDPRExportReport gdprReport)
        {
            return new GdprReportResponseDTO 
            {
                SystemUuid = new Guid(gdprReport.SystemUuid),
                SystemName = gdprReport.SystemName,
                NoData = gdprReport.NoData,
                PersonalData = gdprReport.PersonalData,
                SensitiveData = gdprReport.SensitiveData,
                LegalData = gdprReport.LegalData,
                BusinessCritical = gdprReport.BusinessCritical?.ToYesNoDontKnowChoice(),
                DataProcessingAgreementConcluded = gdprReport.DataProcessingAgreementConcluded,
                LinkToDirectory = gdprReport.LinkToDirectory,
                SensitiveDataTypes = gdprReport.SensitiveDataTypes,
                RiskAssessment = gdprReport.RiskAssessment?.ToYesNoDontKnowChoice(),
                RiskAssessmentDate = gdprReport.RiskAssessmentDate,
                PlannedRiskAssessmentDate = gdprReport.PlannedRiskAssessmentDate,
                PreRiskAssessment = gdprReport.PreRiskAssessment?.ToRiskLevelChoice(),
                PersonalDataCpr = gdprReport.PersonalDataCpr,
                PersonalDataSocialProblems = gdprReport.PersonalDataSocialProblems,
                PersonalDataSocialOtherPrivateMatters = gdprReport.PersonalDataSocialOtherPrivateMatters,
                DPIA = gdprReport.DPIA?.ToYesNoDontKnowChoice(),
                HostedAt = gdprReport.HostedAt?.ToHostingChoice(),
            };
        }
    }
}