using Core.DomainModel.ItSystem.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Presentation.Web.Models.API.V2.Internal.Response
{
    public class GdprReportResponseDTO
    {
        public Guid SystemUuid { get; set; }
        public string SystemName { get; set; }
        public bool NoData { get; set; }
        public bool SensitiveData { get; set; }
        public bool LegalData { get; set; }
        public DataOptions? BusinessCritical { get; set; } //Do something here
        public bool DataProcessingAgreementConcluded { get; set; }
        public bool LinkToDirectory { get; set; }
        public IEnumerable<string> SensitiveDataTypes { get; set; }
        public DataOptions? RiskAssessment { get; set; } //Do something here
        public DateTime? RiskAssessmentDate { get; set; } //Do something here
        public DateTime? PlannedRiskAssessmentDate { get; set; } //Do something here
        public RiskLevel? PreRiskAssessment { get; set; } //Do something here
        public bool PersonalDataCpr { get; set; }
        public bool PersonalDataSocialProblems { get; set; }
        public bool PersonalDataSocialOtherPrivateMatters { get; set; }
        public DataOptions? DPIA { get; set; } //Do something here
        public HostedAt? HostedAt { get; set; } //Do something here
    }
}