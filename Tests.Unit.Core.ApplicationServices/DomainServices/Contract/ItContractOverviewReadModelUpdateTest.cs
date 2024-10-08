﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItContract.Read;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainModel.Shared;
using Core.DomainServices;
using Core.DomainServices.Contract;
using Core.DomainServices.Mapping;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.DomainServices.Contract
{
    public class ItContractOverviewReadModelUpdateTest : WithAutoFixture
    {
        private readonly ItContractOverviewReadModelUpdate _sut;
        private readonly Mock<IGenericRepository<ItContractOverviewRoleAssignmentReadModel>> _roleAssignmentReposioryMock;
        private readonly Mock<IGenericRepository<ItContractOverviewReadModelDataProcessingAgreement>> _dprAssignmentRepositoryMock;
        private readonly Mock<IGenericRepository<ItContractOverviewReadModelItSystemUsage>> _systemUsageAssignmentRepositoryMock;
        private readonly Mock<IGenericRepository<ItContractOverviewReadModelSystemRelation>> _associatedSystemRelationRepositoryMock;

        public ItContractOverviewReadModelUpdateTest()
        {
            _roleAssignmentReposioryMock = new Mock<IGenericRepository<ItContractOverviewRoleAssignmentReadModel>>();
            _dprAssignmentRepositoryMock = new Mock<IGenericRepository<ItContractOverviewReadModelDataProcessingAgreement>>();
            _systemUsageAssignmentRepositoryMock = new Mock<IGenericRepository<ItContractOverviewReadModelItSystemUsage>>();
            _associatedSystemRelationRepositoryMock = new Mock<IGenericRepository<ItContractOverviewReadModelSystemRelation>>();
            _sut = new ItContractOverviewReadModelUpdate(_roleAssignmentReposioryMock.Object, _dprAssignmentRepositoryMock.Object, _systemUsageAssignmentRepositoryMock.Object, _associatedSystemRelationRepositoryMock.Object);
        }

        [Fact]
        public void Apply_Can_Map_Parent_And_Org_Relations()
        {
            //Arrange
            var itContract = new ItContract
            {
                Id = A<int>(),
                OrganizationId = A<int>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.Id, itContractOverviewReadModel.SourceEntityId);
            Assert.Equal(itContract.OrganizationId, itContractOverviewReadModel.OrganizationId);
        }

        [Fact]
        public void Apply_Can_Map_Name()
        {
            //Arrange
            var itContract = new ItContract
            {
                Name = A<string>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.Name, itContractOverviewReadModel.Name);
        }

        [Fact]
        public void Apply_Can_Map_IsActive_To_False_When_Invalid_Due_To_Date_Period()
        {
            //Arrange
            var itContract = new ItContract
            {
                Concluded = DateTime.Today.AddDays(1)
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.False(itContractOverviewReadModel.IsActive);
        }

        [Fact]
        public void Apply_Can_Map_IsActive_To_True_When_Enforced_And_Invalid_Due_To_Date_Period()
        {
            //Arrange
            var itContract = new ItContract
            {
                Concluded = DateTime.Today.AddDays(1),
                Active = true
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.True(itContractOverviewReadModel.IsActive);
        }

        [Fact]
        public void Apply_Can_Map_IsActive_To_True_Due_To_Date_Period()
        {
            //Arrange
            var itContract = new ItContract
            {
                Concluded = DateTime.Today.AddDays(-1)
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.True(itContractOverviewReadModel.IsActive);
        }

        [Fact]
        public void Apply_Can_Map_ContractId()
        {
            //Arrange
            var itContract = new ItContract
            {
                ItContractId = A<string>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.ItContractId, itContractOverviewReadModel.ContractId);
        }

        [Fact]
        public void Apply_Can_Map_ContractSigner()
        {
            //Arrange
            var itContract = new ItContract
            {
                ContractSigner = A<string>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.ContractSigner, itContractOverviewReadModel.ContractSigner);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ProcurementInitiated(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                ProcurementInitiated = isNull ? null : A<YesNoUndecidedOption>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.ProcurementInitiated, itContractOverviewReadModel.ProcurementInitiated);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_OperationRemunerationBegunDate(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                OperationRemunerationBegun = isNull ? null : A<DateTime>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.OperationRemunerationBegun?.Date, itContractOverviewReadModel.OperationRemunerationBegunDate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_IrrevocableTo(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                IrrevocableTo = isNull ? null : A<DateTime>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.IrrevocableTo?.Date, itContractOverviewReadModel.IrrevocableTo);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_TerminatedAt(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                Terminated = isNull ? null : A<DateTime>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.Terminated?.Date, itContractOverviewReadModel.TerminatedAt);
        }

        [Fact]
        public void Apply_Can_Map_LastEditedAtDate()
        {
            //Arrange
            var itContract = new ItContract
            {
                LastChanged = A<DateTime>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.LastChanged.Date, itContractOverviewReadModel.LastEditedAtDate);
        }

        [Fact]
        public void Apply_Can_LastChangedByUser()
        {
            //Arrange
            var itContract = new ItContract
            {
                LastChangedByUser = new User
                {
                    Name = A<string>(),
                    LastName = A<string>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.LastChangedByUser.GetFullName(), itContractOverviewReadModel.LastEditedByUserName);
        }

        [Fact]
        public void Apply_Can_LastChangedByUserId()
        {
            //Arrange
            var itContract = new ItContract
            {
                LastChangedByUserId = A<int>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.LastChangedByUserId, itContractOverviewReadModel.LastEditedByUserId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_Concluded(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                Concluded = isNull ? null : A<DateTime>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.Concluded?.Date, itContractOverviewReadModel.Concluded);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ExpirationDate(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                ExpirationDate = isNull ? null : A<DateTime>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.ExpirationDate?.Date, itContractOverviewReadModel.ExpirationDate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_Supplier(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                Supplier = isNull ? null : new Organization()
                {
                    Id = A<int>(),
                    Name = A<string>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.SupplierId);
                Assert.Null(itContractOverviewReadModel.SupplierName);
            }
            else
            {
                Assert.Equal(itContract.Supplier.Id, itContractOverviewReadModel.SupplierId);
                Assert.Equal(itContract.Supplier.Name, itContractOverviewReadModel.SupplierName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ParentContract(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                Parent = isNull ? null : new ItContract()
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.ParentContractName);
                Assert.Null(itContractOverviewReadModel.ParentContractUuid);
                Assert.Null(itContractOverviewReadModel.ParentContractId);
            }
            else
            {
                Assert.Equal(itContract.Parent.Id, itContractOverviewReadModel.ParentContractId);
                Assert.Equal(itContract.Parent.Name, itContractOverviewReadModel.ParentContractName);
                Assert.Equal(itContract.Parent.Uuid, itContractOverviewReadModel.ParentContractUuid);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_Criticality(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                Criticality = isNull ? null : new CriticalityType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.CriticalityId);
                Assert.Null(itContractOverviewReadModel.CriticalityUuid);
                Assert.Null(itContractOverviewReadModel.CriticalityName);
            }
            else
            {
                Assert.Equal(itContract.Criticality.Id, itContractOverviewReadModel.CriticalityId);
                Assert.Equal(itContract.Criticality.Uuid, itContractOverviewReadModel.CriticalityUuid);
                Assert.Equal(itContract.Criticality.Name, itContractOverviewReadModel.CriticalityName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ResponsibleOrgUnit(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                ResponsibleOrganizationUnit = isNull ? null : new OrganizationUnit
                {
                    Id = A<int>(),
                    Name = A<string>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.ResponsibleOrgUnitId);
                Assert.Null(itContractOverviewReadModel.ResponsibleOrgUnitName);
            }
            else
            {
                Assert.Equal(itContract.ResponsibleOrganizationUnit.Id, itContractOverviewReadModel.ResponsibleOrgUnitId);
                Assert.Equal(itContract.ResponsibleOrganizationUnit.Name, itContractOverviewReadModel.ResponsibleOrgUnitName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ContractType(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                ContractType = isNull ? null : new ItContractType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.ContractTypeId);
                Assert.Null(itContractOverviewReadModel.ContractTypeUuid);
                Assert.Null(itContractOverviewReadModel.ContractTypeName);
            }
            else
            {
                Assert.Equal(itContract.ContractType.Id, itContractOverviewReadModel.ContractTypeId);
                Assert.Equal(itContract.ContractType.Uuid, itContractOverviewReadModel.ContractTypeUuid);
                Assert.Equal(itContract.ContractType.Name, itContractOverviewReadModel.ContractTypeName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ContractTemplate(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                ContractTemplate = isNull ? null : new ItContractTemplateType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.ContractTemplateId);
                Assert.Null(itContractOverviewReadModel.ContractTemplateUuid);
                Assert.Null(itContractOverviewReadModel.ContractTemplateName);
            }
            else
            {
                Assert.Equal(itContract.ContractTemplate.Id, itContractOverviewReadModel.ContractTemplateId);
                Assert.Equal(itContract.ContractTemplate.Uuid, itContractOverviewReadModel.ContractTemplateUuid);
                Assert.Equal(itContract.ContractTemplate.Name, itContractOverviewReadModel.ContractTemplateName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_PurchaseForm(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                PurchaseForm = isNull ? null : new PurchaseFormType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.PurchaseFormId);
                Assert.Null(itContractOverviewReadModel.PurchaseFormUuid);
                Assert.Null(itContractOverviewReadModel.PurchaseFormName);
            }
            else
            {
                Assert.Equal(itContract.PurchaseForm.Id, itContractOverviewReadModel.PurchaseFormId);
                Assert.Equal(itContract.PurchaseForm.Uuid, itContractOverviewReadModel.PurchaseFormUuid);
                Assert.Equal(itContract.PurchaseForm.Name, itContractOverviewReadModel.PurchaseFormName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ProcurementStrategy(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                ProcurementStrategy = isNull ? null : new ProcurementStrategyType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.ProcurementStrategyId);
                Assert.Null(itContractOverviewReadModel.ProcurementStrategyUuid);
                Assert.Null(itContractOverviewReadModel.ProcurementStrategyName);
            }
            else
            {
                Assert.Equal(itContract.ProcurementStrategy.Id, itContractOverviewReadModel.ProcurementStrategyId);
                Assert.Equal(itContract.ProcurementStrategy.Uuid, itContractOverviewReadModel.ProcurementStrategyUuid);
                Assert.Equal(itContract.ProcurementStrategy.Name, itContractOverviewReadModel.ProcurementStrategyName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_ProcurementPlan(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                ProcurementPlanYear = isNull ? null : A<int>(),
                ProcurementPlanQuarter = isNull ? null : A<int>()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.ProcurementPlanYear);
                Assert.Null(itContractOverviewReadModel.ProcurementPlanQuarter);
            }
            else
            {
                Assert.Equal(itContract.ProcurementPlanQuarter, itContractOverviewReadModel.ProcurementPlanQuarter);
                Assert.Equal(itContract.ProcurementPlanYear, itContractOverviewReadModel.ProcurementPlanYear);
            }
        }

        [Fact]
        public void Apply_Can_Map_RoleAssignments()
        {
            //Arrange
            var itContract = new ItContract
            {
                Rights = Many<int>().Select(right =>
                {
                    var roleId = A<int>();
                    var userId = A<int>();
                    return new ItContractRight
                    {
                        RoleId = roleId,
                        Role = new ItContractRole
                        {
                            Id = roleId,
                            Name = A<string>()
                        },
                        UserId = userId,
                        User = new User
                        {
                            Id = userId,
                            Name = A<string>(),
                            LastName = A<string>(),
                            Email = A<string>()
                        }
                    };
                }).ToList()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.Rights.Count, itContractOverviewReadModel.RoleAssignments.Count);
            foreach (var itContractRight in itContract.Rights)
            {
                Assert.Contains(itContractOverviewReadModel.RoleAssignments,
                    assignment =>
                        assignment.Email == itContractRight.User.Email &&
                        assignment.RoleId == itContractRight.Role.Id &&
                        assignment.UserFullName == itContractRight.User.GetFullName() &&
                        assignment.UserId == itContractRight.User.Id
                );
            }
        }

        [Fact]
        public void Apply_Can_Map_DataProcessingRegistrations()
        {
            //Arrange
            var dataProcessingRegistrations = Many<string>().Select(name => new DataProcessingRegistration { Id = A<int>(), Name = name, Uuid = A<Guid>()}).ToList();

            //Set all but the first to concluded
            var expectedAgreements = dataProcessingRegistrations.Skip(1).ToList();
            expectedAgreements.ForEach(x => x.SetIsAgreementConcluded(YesNoIrrelevantOption.YES));

            var itContract = new ItContract
            {
                DataProcessingRegistrations = dataProcessingRegistrations
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(expectedAgreements.Count, itContractOverviewReadModel.DataProcessingAgreements.Count);
            foreach (var dataProcessingRegistration in expectedAgreements)
            {
                Assert.Contains(itContractOverviewReadModel.DataProcessingAgreements,
                    rm =>
                        rm.DataProcessingRegistrationId == dataProcessingRegistration.Id &&
                        rm.DataProcessingRegistrationName == dataProcessingRegistration.Name &&
                        rm.DataProcessingRegistrationUuid == dataProcessingRegistration.Uuid
                );
            }
            Assert.Equal(string.Join(", ", expectedAgreements.Select(x => x.Name)), itContractOverviewReadModel.DataProcessingAgreementsCsv);
        }

        [Fact]
        public void Apply_Can_Map_ItSystemUsages()
        {
            //Arrange
            var systemUsages = Many<string>().Select(name => new ItContractItSystemUsage()
            {
                ItSystemUsage = new()
                {
                    Id = A<int>(),
                    Uuid = A<Guid>(),
                    ItSystem = new ItSystem()
                    {
                        Id = A<int>(),
                        Name = name,
                        Disabled = A<bool>()
                    }
                }
            }).ToList();

            var itContract = new ItContract
            {
                AssociatedSystemUsages = systemUsages
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(systemUsages.Count, itContractOverviewReadModel.ItSystemUsages.Count);
            foreach (var systemUsage in systemUsages)
            {
                Assert.Contains(itContractOverviewReadModel.ItSystemUsages,
                    rm =>
                        rm.ItSystemUsageId == systemUsage.ItSystemUsage.Id &&
                        rm.ItSystemUsageSystemUuid == systemUsage.ItSystemUsage.ItSystem.Uuid.ToString("D") &&
                        rm.ItSystemUsageName == systemUsage.ItSystemUsage.ItSystem.Name &&
                        rm.ItSystemUsageUuid == systemUsage.ItSystemUsage.Uuid &&
                        rm.ItSystemIsDisabled == systemUsage.ItSystemUsage.ItSystem.Disabled
                );
            }
            Assert.Equal(string.Join(", ", systemUsages.Select(x => x.ItSystemUsage.MapItSystemName())), itContractOverviewReadModel.ItSystemUsagesCsv);
            Assert.Equal(string.Join(", ", systemUsages.Select(x => x.ItSystemUsage.ItSystem.Uuid.ToString("D"))), itContractOverviewReadModel.ItSystemUsagesSystemUuidCsv);
        }

        [Fact]
        public void Apply_Can_Map_SystemRelations()
        {
            //Arrange
            var systemRelations = Many<int>().Select(relationId => new SystemRelation(new ItSystemUsage())
            {
                Id = relationId,
                FromSystemUsageId = A<int>(),
                ToSystemUsageId = A<int>(),

            }).ToList();

            var itContract = new ItContract
            {
                AssociatedSystemRelations = systemRelations
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(systemRelations.Count, itContractOverviewReadModel.NumberOfAssociatedSystemRelations);
            Assert.Equal(systemRelations.Count, itContractOverviewReadModel.SystemRelations.Count);
            foreach (var systemRelation in systemRelations)
            {
                Assert.Contains(itContractOverviewReadModel.SystemRelations,
                    rm =>
                        rm.RelationId == systemRelation.Id &&
                        rm.FromSystemUsageId == systemRelation.FromSystemUsageId &&
                        rm.ToSystemUsageId == systemRelation.ToSystemUsageId
                );
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_Reference(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                Reference = isNull ? null : new ExternalReference
                {
                    Title = A<string>(),
                    URL = A<string>(),
                    ExternalReferenceId = A<string>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.ActiveReferenceExternalReferenceId);
                Assert.Null(itContractOverviewReadModel.ActiveReferenceTitle);
                Assert.Null(itContractOverviewReadModel.ActiveReferenceUrl);
            }
            else
            {
                Assert.Equal(itContract.Reference.ExternalReferenceId, itContractOverviewReadModel.ActiveReferenceExternalReferenceId);
                Assert.Equal(itContract.Reference.Title, itContractOverviewReadModel.ActiveReferenceTitle);
                Assert.Equal(itContract.Reference.URL, itContractOverviewReadModel.ActiveReferenceUrl);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_PaymentModel(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                PaymentModel = isNull ? null : new PaymentModelType
                {
                    Id = A<int>(),
                    Name = A<string>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.PaymentModelId);
                Assert.Null(itContractOverviewReadModel.PaymentModelUuid);
                Assert.Null(itContractOverviewReadModel.PaymentModelName);
            }
            else
            {
                Assert.Equal(itContract.PaymentModel.Id, itContractOverviewReadModel.PaymentModelId);
                Assert.Equal(itContract.PaymentModel.Uuid, itContractOverviewReadModel.PaymentModelUuid);
                Assert.Equal(itContract.PaymentModel.Name, itContractOverviewReadModel.PaymentModelName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_PaymentFrequencey(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                PaymentFreqency = isNull ? null : new PaymentFreqencyType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.PaymentFrequencyId);
                Assert.Null(itContractOverviewReadModel.PaymentFrequencyUuid);
                Assert.Null(itContractOverviewReadModel.PaymentFrequencyName);
            }
            else
            {
                Assert.Equal(itContract.PaymentFreqency.Id, itContractOverviewReadModel.PaymentFrequencyId);
                Assert.Equal(itContract.PaymentFreqency.Uuid, itContractOverviewReadModel.PaymentFrequencyUuid);
                Assert.Equal(itContract.PaymentFreqency.Name, itContractOverviewReadModel.PaymentFrequencyName);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_OptionExtend(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                OptionExtend = isNull ? null : new OptionExtendType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.OptionExtendId);
                Assert.Null(itContractOverviewReadModel.OptionExtendUuid);
                Assert.Null(itContractOverviewReadModel.OptionExtendName);
            }
            else
            {
                Assert.Equal(itContract.OptionExtend.Id, itContractOverviewReadModel.OptionExtendId);
                Assert.Equal(itContract.OptionExtend.Uuid, itContractOverviewReadModel.OptionExtendUuid);
                Assert.Equal(itContract.OptionExtend.Name, itContractOverviewReadModel.OptionExtendName);
            }
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Apply_Can_Map_TerminationDeadline(bool isNull)
        {
            //Arrange
            var itContract = new ItContract
            {
                TerminationDeadline = isNull ? null : new TerminationDeadlineType
                {
                    Id = A<int>(),
                    Name = A<string>(),
                    Uuid = A<Guid>()
                }
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            if (isNull)
            {
                Assert.Null(itContractOverviewReadModel.TerminationDeadlineId);
                Assert.Null(itContractOverviewReadModel.TerminationDeadlineUuid);
                Assert.Null(itContractOverviewReadModel.TerminationDeadlineName);
            }
            else
            {
                Assert.Equal(itContract.TerminationDeadline.Id, itContractOverviewReadModel.TerminationDeadlineId);
                Assert.Equal(itContract.TerminationDeadline.Uuid, itContractOverviewReadModel.TerminationDeadlineUuid);
                Assert.Equal(itContract.TerminationDeadline.Name, itContractOverviewReadModel.TerminationDeadlineName);
            }
        }

        [Theory]
        [InlineData(false, null, null, "")]
        [InlineData(true, null, null, "Løbende")]
        [InlineData(false, 2, null, "2 år")]
        [InlineData(false, 2, 1, "2 år og 1 måned")]
        [InlineData(false, 2, 3, "2 år og 3 måneder")]
        [InlineData(false, null, 3, "3 måneder")]
        [InlineData(false, null, 1, "1 måned")]
        public void Apply_Can_Map_Duration(bool ongoing, int? years, int? months, string expectedResult)
        {
            //Arrange
            var itContract = new ItContract
            {
                DurationOngoing = ongoing,
                DurationYears = years,
                DurationMonths = months
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(expectedResult, itContractOverviewReadModel.Duration);
        }

        [Fact]
        public void Apply_Can_Map_Economy()
        {
            //Arrange
            var itContract = new ItContract
            {
                InternEconomyStreams = Many<int>().Select(_ => new EconomyStream
                {
                    Acquisition = 1000000,
                    Other = 1000000,
                    Operation = 1000000,
                }).ToList(), //Add the internals to ensure they do NOT affect the result of the computation if present
                ExternEconomyStreams = Many<int>().Select(_ => new EconomyStream
                {
                    Acquisition = A<int>() % 1000,
                    Other = A<int>() % 1000,
                    Operation = A<int>() % 1000,
                    AuditStatus = A<TrafficLight>(),
                    AuditDate = A<DateTime>()
                }).ToList()
            };
            var itContractOverviewReadModel = new ItContractOverviewReadModel();

            //Act
            _sut.Apply(itContract, itContractOverviewReadModel);

            //Assert
            Assert.Equal(itContract.ExternEconomyStreams.Sum(x => x.Operation), itContractOverviewReadModel.AccumulatedOperationCost);
            Assert.Equal(itContract.ExternEconomyStreams.Sum(x => x.Other), itContractOverviewReadModel.AccumulatedOtherCost);
            Assert.Equal(itContract.ExternEconomyStreams.Sum(x => x.Acquisition), itContractOverviewReadModel.AccumulatedAcquisitionCost);
            Assert.Equal(itContract.ExternEconomyStreams.OrderByDescending(x => x.AuditDate.GetValueOrDefault()).First().AuditDate?.Date, itContractOverviewReadModel.LatestAuditDate);
            var sourceAuditStatuses = itContract.ExternEconomyStreams.GroupBy(x => x.AuditStatus).ToDictionary(x => x.Key, x => x.Count());
            AssertAuditStatus(sourceAuditStatuses, TrafficLight.Green, itContractOverviewReadModel.AuditStatusGreen);
            AssertAuditStatus(sourceAuditStatuses, TrafficLight.Red, itContractOverviewReadModel.AuditStatusRed);
            AssertAuditStatus(sourceAuditStatuses, TrafficLight.Yellow, itContractOverviewReadModel.AuditStatusYellow);
            AssertAuditStatus(sourceAuditStatuses, TrafficLight.White, itContractOverviewReadModel.AuditStatusWhite);
        }

        private static void AssertAuditStatus(Dictionary<TrafficLight, int> sourceAuditStatuses, TrafficLight statusType, int mappedResult)
        {
            Assert.Equal(sourceAuditStatuses.ContainsKey(statusType) ? sourceAuditStatuses[statusType] : 0, mappedResult);
        }
    }
}
