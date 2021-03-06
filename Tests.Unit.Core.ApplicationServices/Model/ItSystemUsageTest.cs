﻿using System;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.ItSystemUsage.GDPR;
using Core.DomainModel.Result;
using Infrastructure.Services.Types;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Model
{
    public class ItSystemUsageTest : WithAutoFixture
    {
        private readonly ItSystemUsage _sut;

        public ItSystemUsageTest()
        {
            _sut = new ItSystemUsage
            {
                Id = A<int>(),
                OrganizationId = A<int>()
            };
        }

        [Fact]
        public void AddUsageRelationTo_Throws_If_Destination_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.AddUsageRelationTo(null, Maybe<ItInterface>.None,
                A<string>(), A<string>(), Maybe<RelationFrequencyType>.None, Maybe<ItContract>.None));
        }

        [Fact]
        public void AddUsageRelationTo_Returns_Error_If_Destination_Equals_Self()
        {
            //Arrange
            var destination = new ItSystemUsage
            {
                Id = _sut.Id
            };

            //Act
            var result = _sut.AddUsageRelationTo(destination, Maybe<ItInterface>.None, A<string>(), A<string>(), Maybe<RelationFrequencyType>.None, Maybe<ItContract>.None);

            //Assert
            AssertErrorResult(result, "'From' cannot equal 'To'", OperationFailure.BadInput);
        }

        [Fact]
        public void AddUsageRelationTo_Returns_Error_If_Organization_Destination_Is_Different()
        {
            //Arrange
            var destination = new ItSystemUsage
            {
                Id = A<int>(),
                OrganizationId = _sut.OrganizationId + 1
            };

            //Act
            var result = _sut.AddUsageRelationTo(destination, Maybe<ItInterface>.None, A<string>(), A<string>(), Maybe<RelationFrequencyType>.None, Maybe<ItContract>.None);

            //Assert
            AssertErrorResult(result, "Attempt to create relation to it-system in a different organization", OperationFailure.BadInput);
        }

        [Fact]
        public void AddUsageRelationTo_Returns_Error_If_Contract_Organization_Is_Different()
        {
            //Arrange
            var destination = new ItSystemUsage
            {
                Id = A<int>(),
                OrganizationId = _sut.OrganizationId
            };

            var itContract = new ItContract { OrganizationId = _sut.OrganizationId + 1 };

            //Act
            var result = _sut.AddUsageRelationTo(destination, Maybe<ItInterface>.None, A<string>(), A<string>(), Maybe<RelationFrequencyType>.None, itContract);

            //Assert
            AssertErrorResult(result, "Attempt to create relation to it contract in a different organization", OperationFailure.BadInput);
        }

        [Fact]
        public void AddUsageRelationTo_Returns_Error_If_Selected_Interface_Is_Not_Exposed_By_Target_System()
        {
            //Arrange
            var interfaceId = A<int>();

            var destination = new ItSystemUsage
            {
                Id = A<int>(),
                OrganizationId = _sut.OrganizationId,
                ItSystem = new ItSystem
                {
                    ItInterfaceExhibits =
                    {
                        new ItInterfaceExhibit
                        {
                            ItInterface = new ItInterface
                            {
                                Id = interfaceId + 1
                            }
                        }
                    }
                }
            };
            var itContract = new ItContract { OrganizationId = _sut.OrganizationId };


            //Act
            var result = _sut.AddUsageRelationTo(destination, new ItInterface(){Id = interfaceId}, A<string>(), A<string>(), Maybe<RelationFrequencyType>.None, itContract);

            //Assert
            AssertErrorResult(result, "Cannot set interface which is not exposed by the 'to' system", OperationFailure.BadInput);
        }

        [Fact]
        public void AddUsageRelationTo_Returns_Success_And_Adds_New_Relation()
        {
            //Arrange
            var interfaceId = A<int>();
            _sut.UsageRelations.Add(new SystemRelation(new ItSystemUsage()));
            var itInterface = new ItInterface
            {
                Id = interfaceId
            };
            var destination = new ItSystemUsage
            {
                Id = A<int>(),
                OrganizationId = _sut.OrganizationId,
                ItSystem = new ItSystem
                {
                    ItInterfaceExhibits =
                    {
                        new ItInterfaceExhibit
                        {
                            ItInterface = itInterface
                        }
                    }
                }
            };
            var itContract = new ItContract { OrganizationId = _sut.OrganizationId };
            var frequencyType = new RelationFrequencyType();
            var description = A<string>();
            var reference = A<string>();

            //Act
            var result = _sut.AddUsageRelationTo(destination, itInterface, description, reference, frequencyType, itContract);

            //Assert
            Assert.True(result.Ok);
            var newRelation = result.Value;
            Assert.True(_sut.UsageRelations.Contains(newRelation));
            Assert.Equal(2, _sut.UsageRelations.Count); //existing + the new one
            Assert.Equal(itContract, newRelation.AssociatedContract);
            Assert.Equal(frequencyType, newRelation.UsageFrequency);
            Assert.Equal(destination, newRelation.ToSystemUsage);
            Assert.Equal(description, newRelation.Description);
            Assert.NotNull(newRelation.Reference);
            Assert.Equal(reference, newRelation.Reference);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetUsageRelation_Returns(bool some)
        {
            //Arrange
            var id = A<int>();
            var systemRelation = new SystemRelation(new ItSystemUsage()) { Id = some ? id : A<int>() };
            var ignoredRelation = new SystemRelation(new ItSystemUsage()) { Id = A<int>() };
            _sut.UsageRelations.Add(ignoredRelation);
            _sut.UsageRelations.Add(systemRelation);

            //Act
            var relation = _sut.GetUsageRelation(id);

            //Assert
            Assert.Equal(some, relation.HasValue);
            if (some)
            {
                Assert.Same(systemRelation, relation.Value);
            }
        }

        [Fact]
        public void RemoveUsageRelation_Returns_NotFound()
        {
            //Arrange
            var id = A<int>();
            var ignoredRelation = new SystemRelation(new ItSystemUsage()) { Id = A<int>() };
            _sut.UsageRelations.Add(ignoredRelation);

            //Act
            var result = _sut.RemoveUsageRelation(id);

            //Assert
            Assert.False(result.Ok);
            Assert.Equal(OperationFailure.NotFound, result.Error);
        }

        [Fact]
        public void RemoveUsageRelation_Returns_Ok()
        {
            //Arrange
            var id = A<int>();
            var removedRelation = new SystemRelation(new ItSystemUsage()) { Id = id };
            var ignoredRelation = new SystemRelation(new ItSystemUsage()) { Id = A<int>() };
            _sut.UsageRelations.Add(ignoredRelation);
            _sut.UsageRelations.Add(removedRelation);

            //Act
            var result = _sut.RemoveUsageRelation(id);

            //Assert
            Assert.True(result.Ok);
            Assert.Same(removedRelation, result.Value);
        }

        private static void AssertErrorResult(Result<SystemRelation, OperationError> result, string message, OperationFailure error)
        {
            Assert.False(result.Ok);
            var operationError = result.Error;
            Assert.Equal(error, operationError.FailureType);
            Assert.True(operationError.Message.HasValue);
            Assert.Equal(message, operationError.Message.Value);
        }

        [Theory]
        [InlineData(SensitiveDataLevel.NONE)]
        [InlineData(SensitiveDataLevel.PERSONALDATA)]
        [InlineData(SensitiveDataLevel.SENSITIVEDATA)]
        [InlineData(SensitiveDataLevel.LEGALDATA)]
        public void AddSensitiveData_Returns_Ok(SensitiveDataLevel sensitiveDataLevel)
        {
            //Act
            var result = _sut.AddSensitiveDataLevel(sensitiveDataLevel);

            //Assert
            Assert.True(result.Ok);
            var usageSensitiveDataLevel = result.Value;
            Assert.Equal(sensitiveDataLevel, usageSensitiveDataLevel.SensitivityDataLevel);
            Assert.Single(_sut.SensitiveDataLevels);

        }

        [Fact]
        public void AddSensitiveData_Fails_With_Conflict_If_Sensitivity_Level_Already_Exists()
        {
            //Arrange
            var preAddedSensitiveDataLevel = new ItSystemUsageSensitiveDataLevel()
            {
                SensitivityDataLevel = SensitiveDataLevel.NONE
            };
            _sut.SensitiveDataLevels.Add(preAddedSensitiveDataLevel);

            //Act
            var result = _sut.AddSensitiveDataLevel(SensitiveDataLevel.NONE);

            //Assert
            AssertErrorResult(result, "Data sensitivity level already exists", OperationFailure.Conflict);
        }

        [Theory]
        [InlineData(SensitiveDataLevel.NONE)]
        [InlineData(SensitiveDataLevel.PERSONALDATA)]
        [InlineData(SensitiveDataLevel.SENSITIVEDATA)]
        [InlineData(SensitiveDataLevel.LEGALDATA)]
        public void RemoveSensitiveData_Returns_Ok(SensitiveDataLevel sensitiveDataLevel)
        {
            //Arrange
            var preAddedSensitiveDataLevel = new ItSystemUsageSensitiveDataLevel()
            {
                ItSystemUsage = _sut,
                SensitivityDataLevel = sensitiveDataLevel
            };
            _sut.SensitiveDataLevels.Add(preAddedSensitiveDataLevel);

            //Act
            var result = _sut.RemoveSensitiveDataLevel(sensitiveDataLevel);

            //Assert
            Assert.True(result.Ok);
            var usageSensitiveDataLevel = result.Value;
            Assert.Equal(preAddedSensitiveDataLevel, usageSensitiveDataLevel);
            Assert.Empty(_sut.SensitiveDataLevels);

        }

        [Fact]
        public void RemoveSensitiveData_Fails_With_NotFound_If_Sensitivity_Level_Does_Not_Exist()
        {
            //Act
            var result = _sut.RemoveSensitiveDataLevel(SensitiveDataLevel.NONE);

            //Assert
            AssertErrorResult(result, "Data sensitivity does not exists on system usage", OperationFailure.NotFound);
        }

        private static void AssertErrorResult(Result<ItSystemUsageSensitiveDataLevel, OperationError> result, string message, OperationFailure error)
        {
            Assert.False(result.Ok);
            var operationError = result.Error;
            Assert.Equal(error, operationError.FailureType);
            Assert.True(operationError.Message.HasValue);
            Assert.Equal(message, operationError.Message.Value);
        }
    }
}
