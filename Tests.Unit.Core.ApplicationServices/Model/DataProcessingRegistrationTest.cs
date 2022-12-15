﻿using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItContract;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Model
{
    public class DataProcessingRegistrationTest : WithAutoFixture
    {
        [Fact]
        public void Can_AssignMainContract()
        {
            //Arrange
            var contractId = A<int>();
            var contract = new ItContract
            {
                Id = contractId
            };
            var dpr = new DataProcessingRegistration()
            {
                AssociatedContracts = new List<ItContract>{ contract }
            };

            //Act
            var error = dpr.AssignMainContract(contractId);

            //Assert
            Assert.False(error.HasValue);
            Assert.NotNull(dpr.MainContract);
            Assert.Equal(contractId, dpr.MainContract.Id);
        }

        [Fact]
        public void AssignMainContract_Returns_NotFound_When_Contract_With_Id_Is_Not_Part_Of_AssociatedContract()
        {
            //Arrange
            var contractId = A<int>();
            var dpr = new DataProcessingRegistration
            {
                AssociatedContracts = new List<ItContract>()
            };

            //Act
            var error = dpr.AssignMainContract(contractId);

            //Assert
            Assert.True(error.HasValue);
            Assert.Equal(OperationFailure.NotFound, error.Value.FailureType);
            Assert.Null(dpr.MainContract);
        }

        [Fact]
        public void AssignMainContract_Returns_No_Error_When_Contract_With_Same_Id_Is_Present()
        {
            //Arrange
            var contractId = A<int>();
            var dpr = CreateDprWithMainContract(contractId);

            //Act
            var error = dpr.AssignMainContract(contractId);

            //Assert
            Assert.False(error.HasValue);
            Assert.Equal(contractId, dpr.MainContract.Id);
        }

        [Fact]
        public void Can_RemoveMainContract()
        {
            //Arrange
            var contractId = A<int>();
            var dpr = CreateDprWithMainContract(contractId);

            //Act
            var error = dpr.RemoveMainContract(contractId);

            //Assert
            Assert.False(error.HasValue);
            Assert.Null(dpr.MainContract);
        }

        [Fact]
        public void RemoveMainContract_Returns_No_Error_When_Contract_Is_Already_Null()
        {
            //Arrange
            var contractId = A<int>();
            var dpr = new DataProcessingRegistration();

            //Act
            var error = dpr.RemoveMainContract(contractId);

            //Assert
            Assert.False(error.HasValue);
            Assert.Null(dpr.MainContract);
        }

        [Fact]
        public void RemoveMainContract_Returns_BadInput_When_Contract_Has_Different_Id()
        {
            //Arrange
            var contractId = A<int>();
            var dpr = CreateDprWithMainContract(A<int>());

            //Act
            var error = dpr.RemoveMainContract(contractId);

            //Assert
            Assert.True(error.HasValue);
            Assert.Equal(OperationFailure.BadInput, error.Value.FailureType);
            Assert.NotNull(dpr.MainContract);
        }

        [Fact]
        public void Can_ResetMainContract()
        {
            //Arrange
            var dpr = CreateDprWithMainContract(A<int>());

            //Act
            dpr.ResetMainContract();

            //Assert
            Assert.Null(dpr.MainContract);
        }

        private DataProcessingRegistration CreateDprWithMainContract(int contractId)
        {
            return new DataProcessingRegistration
            {
                MainContract = new ItContract
                {
                    Id = contractId
                }
            };
        }
    }
}
