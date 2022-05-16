﻿using System;
using System.Linq;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Core.DomainServices.Repositories.Organization;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.DomainServices.Repositories
{
    public class OrganizationRepositoryTest : WithAutoFixture
    {
        private readonly Mock<IGenericRepository<Organization>> _repository;
        private readonly Mock<IDomainEvents> _domainEvents;
        private readonly OrganizationRepository _sut;

        public OrganizationRepositoryTest()
        {
            _repository = new Mock<IGenericRepository<Organization>>();
            _domainEvents = new Mock<IDomainEvents>();
            _sut = new OrganizationRepository(_repository.Object, _domainEvents.Object);
        }

        [Fact]
        public void GetByCvr_Returns_Value()
        {
            //Arrange
            var inputId = A<string>();
            var expectedResponse = CreateOrganization(inputId);

            ExpectRepositoryContent(CreateOrganization(), expectedResponse);

            //Act
            var result = _sut.GetByCvr(inputId);

            //Assert
            Assert.True(result.HasValue);
            Assert.Same(expectedResponse, result.Value);
        }

        [Fact]
        public void GetByCvr_Returns_None()
        {
            //Arrange
            var inputId = A<string>();

            ExpectRepositoryContent(CreateOrganization(), CreateOrganization());

            //Act
            var result = _sut.GetByCvr(inputId);

            //Assert
            Assert.False(result.HasValue);
        }

        private void ExpectRepositoryContent(params Organization[] response)
        {
            _repository.Setup(x => x.AsQueryable()).Returns(response.AsQueryable());
        }

        private Organization CreateOrganization(string cvr = null)
        {
            return new Organization
            {
                Cvr = cvr ?? A<string>()
            };
        }
    }
}
