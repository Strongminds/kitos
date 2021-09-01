﻿using System;
using System.Collections.Generic;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared;
using Infrastructure.Services.Types;
using Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations.Mapping;
using Presentation.Web.Models.API.V2.Request.DataProcessing;
using Xunit;

namespace Tests.Unit.Presentation.Web.Models.V2
{
    public class DataProcessingRegistrationWriteModelMapperTest : WriteModelMapperTestBase
    {
        private readonly DataProcessingRegistrationWriteModelMapper _sut;

        public DataProcessingRegistrationWriteModelMapperTest()
        {
            _sut = new DataProcessingRegistrationWriteModelMapper();
        }

        [Fact]
        public void MapGeneral_Returns_UpdatedDataProcessingRegistrationGeneralDataParameters()
        {
            //Arrange
            var input = A<DataProcessingRegistrationGeneralDataWriteRequestDTO>();

            //Act
            var output = _sut.MapGeneral(input);

            //Assert
            AssertGeneralData(input, output);
        }

        [Fact]
        public void MapGeneral__Resets_InsecureCountries_If_Input_Is_Null()
        {
            //Arrange
            var input = A<DataProcessingRegistrationGeneralDataWriteRequestDTO>();
            input.InsecureCountriesSubjectToDataTransferUuids = null;

            //Act
            var output = _sut.MapGeneral(input);

            //Assert
            AssertPropertyContainsResetDataChange(output.InsecureCountriesSubjectToDataTransferUuids);
        }

        [Fact]
        public void MapGeneral__Resets_DataProcessors_If_Input_Is_Null()
        {
            //Arrange
            var input = A<DataProcessingRegistrationGeneralDataWriteRequestDTO>();
            input.DataProcessorUuids = null;

            //Act
            var output = _sut.MapGeneral(input);

            //Assert
            AssertPropertyContainsResetDataChange(output.DataProcessorUuids);
        }

        [Fact]
        public void MapGeneral__Resets_SubDataProcessors_If_Input_Is_Null()
        {
            //Arrange
            var input = A<DataProcessingRegistrationGeneralDataWriteRequestDTO>();
            input.SubDataProcessorUuids = null;

            //Act
            var output = _sut.MapGeneral(input);

            //Assert
            AssertPropertyContainsResetDataChange(output.SubDataProcessorUuids);
        }

        [Fact]
        public void FromPOST_Maps_All_Sections()
        {
            //Arrange
            var input = A<DataProcessingRegistrationWriteRequestDTO>();

            //Act
            var output = _sut.FromPOST(input);

            //Assert
            AssertGeneralData(input.General, output.General.Value);
            Assert.Equal(input.SystemUsageUuids, output.SystemUsageUuids.Value);
        }

        [Fact]
        public void FromPOST_Ignores_Undefined_Sections()
        {
            //Arrange
            var input = new DataProcessingRegistrationWriteRequestDTO();

            //Act
            var output = _sut.FromPOST(input);

            //Assert undefined sections are ignored
            Assert.True(output.SystemUsageUuids.IsNone);
            Assert.True(output.General.IsNone);
        }

        [Fact]
        public void FromPUT_Maps_All_Sections()
        {
            //Arrange
            var input = A<DataProcessingRegistrationWriteRequestDTO>();

            //Act
            var output = _sut.FromPUT(input);

            //Assert
            AssertGeneralData(input.General, output.General.Value);
            Assert.Equal(input.SystemUsageUuids, output.SystemUsageUuids.Value);
        }

        [Fact]
        public void FromPUT_Assigns_ResetData_To_Undefined_Sections()
        {
            //Arrange
            var input = new DataProcessingRegistrationWriteRequestDTO();

            //Act
            var output = _sut.FromPUT(input);

            //Assert that method patched empty values before mapping
            Assert.NotNull(input.General);
            Assert.NotNull(input.SystemUsageUuids);
            AssertGeneralData(input.General, output.General.Value);
            Assert.Equal(input.SystemUsageUuids, output.SystemUsageUuids.Value);
        }

        private static void AssertGeneralData(DataProcessingRegistrationGeneralDataWriteRequestDTO input, UpdatedDataProcessingRegistrationGeneralDataParameters output)
        {
            Assert.Equal(input.DataResponsibleUuid, AssertPropertyContainsDataChange(output.DataResponsibleUuid));
            Assert.Equal(input.DataResponsibleRemark, AssertPropertyContainsDataChange(output.DataResponsibleRemark));
            Assert.Equal(input.IsAgreementConcluded?.ToYesNoIrrelevantOption(), AssertPropertyContainsDataChange(output.IsAgreementConcluded));
            Assert.Equal(input.IsAgreementConcludedRemark, AssertPropertyContainsDataChange(output.IsAgreementConcludedRemark));
            Assert.Equal(input.AgreementConcludedAt, AssertPropertyContainsDataChange(output.AgreementConcludedAt));
            Assert.Equal(input.BasisForTransferUuid, AssertPropertyContainsDataChange(output.BasisForTransferUuid));
            Assert.Equal(input.TransferToInsecureThirdCountries?.ToYesNoUndecidedOption(), AssertPropertyContainsDataChange(output.TransferToInsecureThirdCountries));
            AssertNullableCollection(input, input.InsecureCountriesSubjectToDataTransferUuids, output.InsecureCountriesSubjectToDataTransferUuids);
            AssertNullableCollection(input, input.DataProcessorUuids, output.DataProcessorUuids);
            Assert.Equal(input.HasSubDataProcessors?.ToYesNoUndecidedOption(), AssertPropertyContainsDataChange(output.HasSubDataProcessors));
            AssertNullableCollection(input, input.SubDataProcessorUuids, output.SubDataProcessorUuids);
        }

        private static void AssertNullableCollection(DataProcessingRegistrationGeneralDataWriteRequestDTO input, IEnumerable<Guid> fromCollection, OptionalValueChange<Maybe<IEnumerable<Guid>>> actualCollection)
        {
            if (fromCollection == null)
                AssertPropertyContainsResetDataChange(actualCollection);
            else
                Assert.Equal(fromCollection,
                    AssertPropertyContainsDataChange(actualCollection));
        }
    }
}
