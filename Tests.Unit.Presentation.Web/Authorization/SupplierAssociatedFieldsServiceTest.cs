using System;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared;
using Core.ApplicationServices.Model.Shared.Write;
using Core.ApplicationServices.Model.SystemUsage.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystem.DataTypes;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.ItSystemUsage.GDPR;
using Core.DomainModel.Shared;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class SupplierAssociatedFieldsServiceTest: WithAutoFixture
    {
        private readonly SupplierAssociatedFieldsService _sut;
        private readonly int _dprId;
        private readonly DataProcessingRegistration _existingDpr;

        public SupplierAssociatedFieldsServiceTest()
        {
           _dprId = A<int>();
           _existingDpr = new DataProcessingRegistration() { Id = _dprId };
           _sut = new SupplierAssociatedFieldsService();
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void
            UsageParams_GivenChangesToAnySupplierAssociatedField_RequestsChangesToSupplierAssociatedFields_ReturnsTrue(
                bool aiTechnology, bool gdprCriticality, bool RiskAssessmentResult)
        {
            var generalProperties = new UpdatedSystemUsageGeneralProperties();
            var gdprProperties = new UpdatedSystemUsageGDPRProperties();
            var parameters = new SystemUsageUpdateParameters();
            if (aiTechnology)
            {
                generalProperties.ContainsAITechnology = A<Maybe<YesNoUndecidedOption>>().AsChangedValue();
                parameters.GeneralProperties = Maybe<UpdatedSystemUsageGeneralProperties>.Some(generalProperties);
            }
            if (gdprCriticality)
            {
                gdprProperties.GdprCriticality = A<GdprCriticality?>().AsChangedValue();
                parameters.GDPR = Maybe<UpdatedSystemUsageGDPRProperties>.Some(gdprProperties);
            }

            if (RiskAssessmentResult)
            {
                gdprProperties.RiskAssessmentResult = A<RiskLevel?>().AsChangedValue();
                parameters.GDPR = Maybe<UpdatedSystemUsageGDPRProperties>.Some(gdprProperties);
            }

            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void
            UsageParams_GivenChangesToAnySupplierAssociatedField_RequestsChangesToNonSupplierAssociatedFields_ReturnsFalse(bool aiTechnology, bool gdprCriticality, bool RiskAssessmentResult)
        {
            var generalProperties = new UpdatedSystemUsageGeneralProperties();
            var gdprProperties = new UpdatedSystemUsageGDPRProperties();
            var parameters = new SystemUsageUpdateParameters();
            var usage = new ItSystemUsage();
            if (aiTechnology)
            {
                generalProperties.ContainsAITechnology = A<Maybe<YesNoUndecidedOption>>().AsChangedValue();
                parameters.GeneralProperties = Maybe<UpdatedSystemUsageGeneralProperties>.Some(generalProperties);
            }
            if (gdprCriticality)
            {
                gdprProperties.GdprCriticality = A<GdprCriticality?>().AsChangedValue();
                parameters.GDPR = Maybe<UpdatedSystemUsageGDPRProperties>.Some(gdprProperties);
            }

            if (RiskAssessmentResult)
            {
                gdprProperties.RiskAssessmentResult = A<RiskLevel?>().AsChangedValue();
                parameters.GDPR = Maybe<UpdatedSystemUsageGDPRProperties>.Some(gdprProperties);
            }

            var result = _sut.RequestsChangesToNonSupplierAssociatedFields(parameters, usage);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false, false)]
        [InlineData(false, false, true, false, false, false, false)]
        [InlineData(false, false, false, true, false, false, false)]
        [InlineData(false, false, false, false, true, false, false)]
        [InlineData(false, false, false, false, false, true, false)]
        [InlineData(false, false, false, false, true, false, true)]
        public void
            UsageParams_GivenChangesToANonSupplierAssociatedField_RequestsChangesToNonSupplierAssociatedFields_ReturnsTrue(
             bool general,
             bool organizationalUsage,
             bool kle,
             bool externalReferences,
             bool roles,
             bool gdpr,
             bool archiving
                )
        {
            var usage = new ItSystemUsage(); 
            var parameters = new SystemUsageUpdateParameters();
            if (general)
            {
                parameters.GeneralProperties = Maybe<UpdatedSystemUsageGeneralProperties>.Some(new UpdatedSystemUsageGeneralProperties()
                {
                    LocalCallName = A<string>().AsChangedValue()
                });
            }
            if (organizationalUsage)
            {
                parameters.OrganizationalUsage = Maybe<UpdatedSystemUsageOrganizationalUseParameters>.Some(new UpdatedSystemUsageOrganizationalUseParameters()
                {
                    UsingOrganizationUnitUuids =  Maybe<IEnumerable<Guid>>.Some(new List<Guid>()).AsChangedValue()
                });
            }
            if (kle)
            {
                parameters.KLE = Maybe<UpdatedSystemUsageKLEDeviationParameters>.Some(new UpdatedSystemUsageKLEDeviationParameters()
                {
                    AddedKLEUuids = Maybe<IEnumerable<Guid>>.Some(new List<Guid>()).AsChangedValue()
                });
            }
            if (externalReferences)
            {
                parameters.ExternalReferences = Maybe<IEnumerable<UpdatedExternalReferenceProperties>>.Some(Many<UpdatedExternalReferenceProperties>());
            }

            if (roles)
            {
                parameters.Roles = Maybe<UpdatedSystemUsageRoles>.Some(new UpdatedSystemUsageRoles()
                {
                    UserRolePairs = Maybe<IEnumerable<UserRolePair>>.Some(new List<UserRolePair>()).AsChangedValue()
                });
            }
            if (gdpr)
            {
                parameters.GDPR = Maybe<UpdatedSystemUsageGDPRProperties>.Some(new UpdatedSystemUsageGDPRProperties()
                {
                    HostedAt = A<HostedAt?>().AsChangedValue()
                });
            }

            if (archiving)
            {
                parameters.Archiving = Maybe<UpdatedSystemUsageArchivingParameters>.Some(new UpdatedSystemUsageArchivingParameters()
                {
                    ArchiveActive = A<bool?>().AsChangedValue()
                });
            }
            
            var result = _sut.RequestsChangesToNonSupplierAssociatedFields(parameters, usage);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false, false)]
        [InlineData(false, false, true, false, false, false, false)]
        [InlineData(false, false, false, true, false, false, false)]
        [InlineData(false, false, false, false, true, false, false)]
        [InlineData(false, false, false, false, false, true, false)]
        [InlineData(false, false, false, false, true, false, true)]
        public void
            UsageParams_GivenChangesToANonSupplierAssociatedField_RequestsChangesToSupplierAssociatedFields_ReturnsFalse(
             bool general,
             bool organizationalUsage,
             bool kle,
             bool externalReferences,
             bool roles,
             bool gdpr,
             bool archiving
                )
        {
            var parameters = new SystemUsageUpdateParameters();
            if (general)
            {
                parameters.GeneralProperties = Maybe<UpdatedSystemUsageGeneralProperties>.Some(new UpdatedSystemUsageGeneralProperties()
                {
                    LocalCallName = A<string>().AsChangedValue()
                });
            }
            if (organizationalUsage)
            {
                parameters.OrganizationalUsage = Maybe<UpdatedSystemUsageOrganizationalUseParameters>.Some(new UpdatedSystemUsageOrganizationalUseParameters()
                {
                    UsingOrganizationUnitUuids = Maybe<IEnumerable<Guid>>.Some(new List<Guid>()).AsChangedValue()
                });
            }
            if (kle)
            {
                parameters.KLE = Maybe<UpdatedSystemUsageKLEDeviationParameters>.Some(new UpdatedSystemUsageKLEDeviationParameters()
                {
                    AddedKLEUuids = Maybe<IEnumerable<Guid>>.Some(new List<Guid>()).AsChangedValue()
                });
            }
            if (externalReferences)
            {
                parameters.ExternalReferences = Maybe<IEnumerable<UpdatedExternalReferenceProperties>>.Some(Many<UpdatedExternalReferenceProperties>());
            }

            if (roles)
            {
                parameters.Roles = Maybe<UpdatedSystemUsageRoles>.Some(new UpdatedSystemUsageRoles()
                {
                    UserRolePairs = Maybe<IEnumerable<UserRolePair>>.Some(new List<UserRolePair>()).AsChangedValue()
                });
            }
            if (gdpr)
            {
                parameters.GDPR = Maybe<UpdatedSystemUsageGDPRProperties>.Some(new UpdatedSystemUsageGDPRProperties()
                {
                    HostedAt = A<HostedAt?>().AsChangedValue()
                });
            }

            if (archiving)
            {
                parameters.Archiving = Maybe<UpdatedSystemUsageArchivingParameters>.Some(new UpdatedSystemUsageArchivingParameters()
                {
                    ArchiveActive = A<bool?>().AsChangedValue()
                });
            }

            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.False(result);
        }

        [Fact]
        public void DprParams_GivenChangesToAnySupplierAssociatedField_RequestsChangesToSupplierAssociatedFields_ReturnsTrue()
        {
            var oversight = new UpdatedDataProcessingRegistrationOversightDataParameters
            {
                IsOversightCompleted = A<YesNoUndecidedOption?>().AsChangedValue()
            };

            var parameters = new DataProcessingRegistrationModificationParameters()
            {
                Oversight = oversight
            };

            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.True(result);
        }

        [Fact]
        public void DprParams_GivenChangesToAnySupplierAssociatedField_RequestsChangesToNonSupplierAssociatedFields_ReturnsFalse()
        {
            var oversight = new UpdatedDataProcessingRegistrationOversightDataParameters
            {
                IsOversightCompleted = A<YesNoUndecidedOption?>().AsChangedValue()
            };

            var parameters = new DataProcessingRegistrationModificationParameters()
            {
                Oversight = oversight
            };

            var result = _sut.RequestsChangesToNonSupplierAssociatedFields(parameters, _existingDpr);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)] 
        public void DprParams_GivenChangesToANonSupplierAssociatedField_RequestsChangesToNonSupplierAssociatedFields_ReturnsTrue(bool addChangeToName, bool addChangeToGeneral,
            bool addChangeToSystemUsageUuids, bool addNonSupplierChangeToOversight, bool addChangeToRoles, bool addChangeToExternalReferences)
        {
            var parameters = new DataProcessingRegistrationModificationParameters();
            if (addChangeToName)
            {
                parameters.Name = A<string>().AsChangedValue();
            }
            if (addChangeToGeneral)
            {
                parameters.General = Maybe<UpdatedDataProcessingRegistrationGeneralDataParameters>.Some(new UpdatedDataProcessingRegistrationGeneralDataParameters());
                parameters.General.Value.AgreementConcludedAt = A<DateTime?>().AsChangedValue();
            }
            if (addChangeToSystemUsageUuids)
            {
                var existingSystemUsages = new List<ItSystemUsage>(){ new(){ Uuid = A<Guid>() }, new(){Uuid = A<Guid>() } };
                _existingDpr.SystemUsages = existingSystemUsages;
                parameters.SystemUsageUuids = Maybe<IEnumerable<Guid>>.Some(Many<Guid>());
            }

            if (addNonSupplierChangeToOversight)
            {
                parameters.Oversight = Maybe<UpdatedDataProcessingRegistrationOversightDataParameters>.Some(new UpdatedDataProcessingRegistrationOversightDataParameters());
                parameters.Oversight.Value.OversightCompletedRemark = A<string>().AsChangedValue();
            }
            if (addChangeToRoles)
            {
                parameters.Roles = Maybe<UpdatedDataProcessingRegistrationRoles>.Some(new UpdatedDataProcessingRegistrationRoles());
                parameters.Roles.Value.UserRolePairs =
                    Maybe<IEnumerable<UserRolePair>>.Some(new List<UserRolePair>()).AsChangedValue();
            }
            if (addChangeToExternalReferences)
            {
                var existingReferences = new List<ExternalReference>(){new ExternalReference(), new ExternalReference()};
                _existingDpr.ExternalReferences = existingReferences;
                parameters.ExternalReferences = Maybe<IEnumerable<UpdatedExternalReferenceProperties>>.Some(Many<UpdatedExternalReferenceProperties>());
            }

            var result = _sut.RequestsChangesToNonSupplierAssociatedFields(parameters, _existingDpr);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)]
        public void DprParams_GivenChangesToANonSupplierAssociatedField_RequestsChangesToSupplierAssociatedFields_ReturnsFalse(bool addChangeToName, bool addChangeToGeneral,
            bool addChangeToSystemUsageUuids, bool addNonSupplierChangeToOversight, bool addChangeToRoles, bool addChangeToExternalReferences)
        {
            var parameters = new DataProcessingRegistrationModificationParameters();
            if (addChangeToName)
            {
                parameters.Name = A<string>().AsChangedValue();
            }
            if (addChangeToGeneral)
            {
                parameters.General = Maybe<UpdatedDataProcessingRegistrationGeneralDataParameters>.Some(new UpdatedDataProcessingRegistrationGeneralDataParameters());
                parameters.General.Value.AgreementConcludedAt = A<DateTime?>().AsChangedValue();
            }
            if (addChangeToSystemUsageUuids)
            {
                var existingSystemUsages = new List<ItSystemUsage>() { new() { Uuid = A<Guid>() }, new() { Uuid = A<Guid>() } };
                _existingDpr.SystemUsages = existingSystemUsages;
                parameters.SystemUsageUuids = Maybe<IEnumerable<Guid>>.Some(Many<Guid>());
            }

            if (addNonSupplierChangeToOversight)
            {
                parameters.Oversight = Maybe<UpdatedDataProcessingRegistrationOversightDataParameters>.Some(new UpdatedDataProcessingRegistrationOversightDataParameters());
                parameters.Oversight.Value.OversightCompletedRemark = A<string>().AsChangedValue();
            }
            if (addChangeToRoles)
            {
                parameters.Roles = Maybe<UpdatedDataProcessingRegistrationRoles>.Some(new UpdatedDataProcessingRegistrationRoles());
                parameters.Roles.Value.UserRolePairs =
                    Maybe<IEnumerable<UserRolePair>>.Some(new List<UserRolePair>()).AsChangedValue();
            }
            if (addChangeToExternalReferences)
            {
                var existingReferences = new List<ExternalReference>() { new ExternalReference(), new ExternalReference() };
                _existingDpr.ExternalReferences = existingReferences;
                parameters.ExternalReferences = Maybe<IEnumerable<UpdatedExternalReferenceProperties>>.Some(Many<UpdatedExternalReferenceProperties>());
            }

            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void
            GivenNoChanges_RequestsChangesToSupplierAssociatedFields_And_RequestsChangesToNonSupplierAssociatedFields_BothReturnFalse(bool dprParams)
        {
            bool requestsChangesToSupplierAssociatedFields;
            bool requestsChangesToNonSupplierAssociatedFields;
            if (dprParams)
            {
                var noChangesParameters = new DataProcessingRegistrationModificationParameters();
                requestsChangesToSupplierAssociatedFields =
                    _sut.RequestsChangesToSupplierAssociatedFields(noChangesParameters);
                requestsChangesToNonSupplierAssociatedFields = _sut.RequestsChangesToNonSupplierAssociatedFields(noChangesParameters, _existingDpr);
            }
            else
            {
                var noChangesParameters = new UpdatedDataProcessingRegistrationOversightDateParameters
                    {
                        CompletedAt = OptionalValueChange<DateTime>.None,
                        Remark = OptionalValueChange<string>.None,
                        OversightReportLink = OptionalValueChange<string>.None
                    };
                requestsChangesToSupplierAssociatedFields =
                    _sut.RequestsChangesToSupplierAssociatedFields(noChangesParameters);
                requestsChangesToNonSupplierAssociatedFields = _sut.RequestsChangesToNonSupplierAssociatedFields(noChangesParameters, _existingDpr);
            }

            Assert.False(requestsChangesToSupplierAssociatedFields);
            Assert.False(requestsChangesToNonSupplierAssociatedFields);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void
            DprOversightDateParams_GivenChangesToSupplierFields_RequestsChangesToSupplierAssociatedFields_Returns_True(bool completedAt, bool remark, bool oversightReportLink)
        {
            var parameters = GetOversightDateParametersWithChange(completedAt, remark, oversightReportLink);

            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.True(result);
        }

        [Fact]
        public void GivenNoChanges_RequestsChangesToSupplierAssociatedFieldsInEnumerable_Returns_False()
        {
            var parametersEnumerable = new List<UpdatedDataProcessingRegistrationOversightDateParameters>()
            {
                new()
                {
                    CompletedAt = OptionalValueChange<DateTime>.None,
                    OversightReportLink = OptionalValueChange<string>.None,
                    Remark = OptionalValueChange<string>.None
                }
            };

            var requestsChangesToSupplierAssociatedFields = _sut.RequestsChangesToSupplierAssociatedFieldsInEnumerable(parametersEnumerable);

            Assert.False(requestsChangesToSupplierAssociatedFields);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void GivenChangeToSupplierField_RequestsChangesToSupplierAssociatedFieldsInEnumerable_Returns_True(
            bool completedAt, bool remark, bool oversightReportLink)
        {
            var parameters = GetOversightDateParametersWithChange(completedAt, remark, oversightReportLink);
            var parametersEnumerable = new List<UpdatedDataProcessingRegistrationOversightDateParameters>()
            {
                parameters
            };
            
            var result = _sut.RequestsChangesToSupplierAssociatedFieldsInEnumerable(parametersEnumerable);

            Assert.True(result);
        }

        private UpdatedDataProcessingRegistrationOversightDateParameters GetOversightDateParametersWithChange(bool completedAt, bool remark, bool oversightReportLink)
        {
            var parameters = new UpdatedDataProcessingRegistrationOversightDateParameters
            {
                CompletedAt = OptionalValueChange<DateTime>.None,
                Remark = OptionalValueChange<string>.None,
                OversightReportLink = OptionalValueChange<string>.None
            };
            if (completedAt)
                parameters.CompletedAt = A<DateTime>().AsChangedValue();
            if (remark)
                parameters.Remark = A<string>().AsChangedValue();
            if (oversightReportLink)
                parameters.OversightReportLink = A<string>().AsChangedValue();

            return parameters;
        }
    }
}
