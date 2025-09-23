﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Organizations;
using Core.ApplicationServices.Organizations.Write;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Generic;
using Infrastructure.Services.DataAccess;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Presentation.Web.Services
{
    public class OrganizationSupplierWriteServiceTest : WithAutoFixture
    {
        private readonly Mock<IGenericRepository<OrganizationSupplier>> _organizationSupplierRepository;
        private readonly Mock<IOrganizationService> _organizationService;
        private readonly Mock<IEntityIdentityResolver> _entityIdentityResolver;
        private readonly Mock<IDomainEvents> _domainEvents;
        private readonly Mock<ITransactionManager> _transactionManager;
        private readonly OrganizationSupplierWriteService _sut;

        public OrganizationSupplierWriteServiceTest()
        {
            _organizationSupplierRepository = new Mock<IGenericRepository<OrganizationSupplier>>();
            _organizationService = new Mock<IOrganizationService>();
            _entityIdentityResolver = new Mock<IEntityIdentityResolver>();
            _domainEvents = new Mock<IDomainEvents>();
            _transactionManager = new Mock<ITransactionManager>();

            _sut = new OrganizationSupplierWriteService(_organizationSupplierRepository.Object,
                _organizationService.Object, _entityIdentityResolver.Object, _domainEvents.Object,
                _transactionManager.Object);
        }

        [Fact]
        public void Can_Get_Suppliers()
        {
            //Arrange
            var organizationUuid = A<Guid>();
            var supplierUuid = A<Guid>();
            var supplier = new Organization
            {
                Uuid = supplierUuid
            };
            var organization = new Organization
            {
                Uuid = organizationUuid,
                Suppliers = new List<OrganizationSupplier> { new OrganizationSupplier { Supplier = supplier } }
            };

            _organizationService.Setup(x => x.GetOrganization(organizationUuid, null))
                .Returns(organization);

            //Act
            var result = _sut.GetSuppliersForOrganization(organizationUuid);

            //Assert
            Assert.True(result.Ok);
            var supplierResult = Assert.Single(result.Value);
            Assert.Equal(supplierUuid, supplierResult.Supplier.Uuid);
        }

        [Fact]
        public void Get_Suppliers_Returns_Error_On_Fail()
        {
            //Arrange
            var organizationUuid = A<Guid>();
            var error = A<OperationError>();

            _organizationService.Setup(x => x.GetOrganization(organizationUuid, null))
                .Returns(error);

            //Act
            var result = _sut.GetSuppliersForOrganization(organizationUuid);

            //Assert
            Assert.True(result.Failed);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void Can_Add_Supplier()
        {
            //Arrange
            var transaction = new Mock<IDatabaseTransaction>();
            var organizationUuid = A<Guid>();
            var supplierUuid = A<Guid>();
            var organizationId = A<int>();
            var supplierId = A<int>();
            var organization = new Organization
            {
                Id = organizationId,
                Uuid = organizationUuid
            };
            var supplier = new Organization
            {
                Id = supplierId,
                Uuid = supplierUuid
            };
            var organizationSupplier = new OrganizationSupplier
            {
                Organization = organization,
                Supplier = supplier
            };

            _organizationService.Setup(x => x.GetOrganization(organizationUuid, null))
                .Returns(organization);
            _entityIdentityResolver.Setup(x => x.ResolveDbId<Organization>(organizationUuid))
                .Returns(organizationId);
            _entityIdentityResolver.Setup(x => x.ResolveDbId<Organization>(supplierUuid))
                .Returns(supplierId);
            _organizationSupplierRepository.Setup(x => x.Insert(It.IsAny<OrganizationSupplier>()))
                .Returns(organizationSupplier);
            _transactionManager.Setup(x => x.Begin()).Returns(transaction.Object);

            //Act
            var result = _sut.AddSupplierToOrganization(organizationUuid, supplierUuid);
            //Assert
            Assert.True(result.Ok);
            var supplierResult = result.Value;
            Assert.Equal(organizationId, supplierResult.Organization.Id);
            Assert.Equal(supplierId, supplierResult.Supplier.Id);
            transaction.Verify(x => x.Commit(), Times.Once);
            _domainEvents.Verify(x => x.Raise(It.IsAny<EntityUpdatedEvent<Organization>>()), Times.Exactly(2));
        }

        [Fact]
        public void Add_Supplier_Fails_If_Supplier_NotFound()
        {
            //Arrange
            var organizationUuid = A<Guid>();
            var supplierUuid = A<Guid>();
            var organizationId = A<int>();
            var organization = new Organization
            {
                Id = organizationId,
                Uuid = organizationUuid
            };

            _organizationService.Setup(x => x.GetOrganization(organizationUuid, null))
                .Returns(organization);
            _entityIdentityResolver.Setup(x => x.ResolveDbId<Organization>(organizationUuid))
                .Returns(organizationId);
            _entityIdentityResolver.Setup(x => x.ResolveDbId<Organization>(supplierUuid))
                .Returns(Maybe<int>.None);

            //Act
            var result = _sut.AddSupplierToOrganization(organizationUuid, supplierUuid);
            //Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.NotFound, result.Error.FailureType);
        }

        [Fact]
        public void Add_Supplier_Fails_If_Organization_NotFound()
        {
            //Arrange
            var organizationUuid = A<Guid>();
            var supplierUuid = A<Guid>();
            var organization = new Organization
            {
                Uuid = organizationUuid
            };

            _organizationService.Setup(x => x.GetOrganization(organizationUuid, null))
                .Returns(organization);
            _entityIdentityResolver.Setup(x => x.ResolveDbId<Organization>(organizationUuid))
                .Returns(Maybe<int>.None);

            //Act
            var result = _sut.AddSupplierToOrganization(organizationUuid, supplierUuid);
            //Assert
            Assert.True(result.Failed);

        }

        [Fact]
        public void Add_Supplier_Fails_If_Organization_Already_Has_Supplier()
        {
            //Arrange
            var organizationUuid = A<Guid>();
            var supplierUuid = A<Guid>();
            var organization = new Organization
            {
                Uuid = organizationUuid,
                Suppliers = new List<OrganizationSupplier>{ new OrganizationSupplier{ Supplier = new Organization {Uuid = supplierUuid}} }
            };

            _organizationService.Setup(x => x.GetOrganization(organizationUuid, null))
                .Returns(organization);

            //Act
            var result = _sut.AddSupplierToOrganization(organizationUuid, supplierUuid);
            //Assert
            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.BadState, result.Error.FailureType);
        }

        [Fact]
        public void Can_Delete_Supplier()
        {
            //Arrange
            var transaction = new Mock<IDatabaseTransaction>();
            var organizationUuid = A<Guid>();
            var supplierUuid = A<Guid>();
            var organizationId = A<int>();
            var supplierId = A<int>();

            var organizationSupplier = new OrganizationSupplier
            {
                OrganizationId = organizationId,
                SupplierId = supplierId
            };

            _organizationSupplierRepository.Setup(x => x.AsQueryable())
                .Returns(new List<OrganizationSupplier>{ organizationSupplier }.AsQueryable());
            _entityIdentityResolver.Setup(x => x.ResolveDbId<Organization>(organizationUuid))
                .Returns(organizationId);
            _entityIdentityResolver.Setup(x => x.ResolveDbId<Organization>(supplierUuid))
                .Returns(supplierId);
            _organizationSupplierRepository.Setup(x => x.Delete(It.IsAny<OrganizationSupplier>()));
            _transactionManager.Setup(x => x.Begin()).Returns(transaction.Object);

            //Act
            var result = _sut.RemoveSupplierFromOrganization(organizationUuid, supplierUuid);

            //Assert
            Assert.True(result.IsNone);
            transaction.Verify(x => x.Commit(), Times.Once);
            _domainEvents.Verify(x => x.Raise(It.IsAny<EntityUpdatedEvent<Organization>>()), Times.Exactly(2));
        }
    }
}
